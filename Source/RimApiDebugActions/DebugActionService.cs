using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace RimApiDebugActions
{
    internal sealed class DebugActionService
    {
        private const string DebugActionAttributeName = "Verse.DebugActionAttribute";

        public object List(string filter)
        {
            var normalizedFilter = (filter ?? string.Empty).Trim();
            var actions = Discover()
                .Where(action => normalizedFilter.Length == 0 || action.Matches(normalizedFilter))
                .OrderBy(action => action.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(action => action.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(action => action.Id, StringComparer.Ordinal)
                .Select(action => action.ToDto())
                .ToArray();

            return new { count = actions.Length, filter = normalizedFilter, actions };
        }

        public object Run(string id)
        {
            var action = Discover().FirstOrDefault(candidate => candidate.Id == id);
            if (action == null)
                return new { success = false, error = "No debug action matched the supplied id.", id };

            if (!action.IsRunnable)
                return new
                {
                    success = false,
                    error = "This endpoint can run only parameterless static void debug actions.",
                    action = action.ToDto()
                };

            try
            {
                var result = action.Method.Invoke(null, Array.Empty<object>());
                return new { success = true, action = action.ToDto(), result };
            }
            catch (TargetInvocationException exception)
            {
                var cause = exception.InnerException ?? exception;
                Log.Error("[RIMAPI Debug Actions] Action '" + action.Id + "' failed: " + cause);
                return new { success = false, action = action.ToDto(), error = cause.Message };
            }
            catch (Exception exception)
            {
                Log.Error("[RIMAPI Debug Actions] Could not invoke action '" + action.Id + "': " + exception);
                return new { success = false, action = action.ToDto(), error = exception.Message };
            }
        }

        private static IEnumerable<ActionInfo> Discover()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types.Where(type => type != null).ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (var type in types)
                {
                    MethodInfo[] methods;
                    try
                    {
                        methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var method in methods)
                    {
                        object[] attributes;
                        try
                        {
                            attributes = method.GetCustomAttributes(false);
                        }
                        catch
                        {
                            continue;
                        }

                        foreach (var attribute in attributes)
                        {
                            if (attribute.GetType().FullName != DebugActionAttributeName)
                                continue;

                            yield return ActionInfo.Create(method, attribute);
                        }
                    }
                }
            }
        }

        private sealed class ActionInfo
        {
            public string Id { get; private set; }
            public string Name { get; private set; }
            public string Category { get; private set; }
            public string Assembly { get; private set; }
            public string DeclaringType { get; private set; }
            public string Signature { get; private set; }
            public bool IsRunnable { get; private set; }
            public MethodInfo Method { get; private set; }

            public static ActionInfo Create(MethodInfo method, object attribute)
            {
                var attributeType = attribute.GetType();
                var category = ReadStringMember(attribute, attributeType, "category", "Category") ?? string.Empty;
                var name = ReadStringMember(attribute, attributeType, "name", "Name") ?? method.Name;
                var signature = method.DeclaringType.FullName + "." + method.Name + "(" +
                    string.Join(",", method.GetParameters().Select(parameter => parameter.ParameterType.FullName)) + ")";

                return new ActionInfo
                {
                    Id = EncodeId(method.Module.Assembly.GetName().Name + "|" + signature),
                    Name = name,
                    Category = category,
                    Assembly = method.Module.Assembly.GetName().Name,
                    DeclaringType = method.DeclaringType.FullName,
                    Signature = signature,
                    IsRunnable = method.IsStatic && method.GetParameters().Length == 0 && method.ReturnType == typeof(void),
                    Method = method
                };
            }

            public bool Matches(string filter)
            {
                return Contains(Name, filter) || Contains(Category, filter) || Contains(Assembly, filter) ||
                       Contains(DeclaringType, filter) || Contains(Signature, filter) || Contains(Id, filter);
            }

            public object ToDto()
            {
                return new
                {
                    id = Id,
                    name = Name,
                    category = Category,
                    assembly = Assembly,
                    declaring_type = DeclaringType,
                    signature = Signature,
                    is_runnable = IsRunnable
                };
            }

            private static bool Contains(string value, string filter)
            {
                return value != null && value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            private static string ReadStringMember(object instance, Type type, string fieldName, string propertyName)
            {
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var field = type.GetField(fieldName, flags);
                if (field != null && field.FieldType == typeof(string))
                    return (string)field.GetValue(instance);

                var property = type.GetProperty(propertyName, flags);
                if (property != null && property.PropertyType == typeof(string) && property.GetIndexParameters().Length == 0)
                    return (string)property.GetValue(instance, null);

                return null;
            }

            private static string EncodeId(string value)
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                    .TrimEnd('=')
                    .Replace('+', '-')
                    .Replace('/', '_');
            }
        }
    }
}
