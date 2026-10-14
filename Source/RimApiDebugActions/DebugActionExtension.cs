using RIMAPI.Core;

namespace RimApiDebugActions
{
    /// <summary>Registers the debug-action and save endpoints with RIMAPI.</summary>
    public sealed class DebugActionExtension : IRimApiExtension
    {
        public string ExtensionId => "debug-actions";
        public string ExtensionName => "RimWorld Debug Actions";
        public string Version => "0.1.2";

        public void RegisterServices(IServiceCollection services)
        {
        }

        public void RegisterEvents(IEventRegistry eventRegistry)
        {
        }

        public void RegisterEndpoints(IExtensionRouter router)
        {
            var service = new DebugActionService();
            router.Get("/debug-actions", context =>
                DebugActionHttpResponse.WriteAsync(context, service.List(context.Request.QueryString["filter"])));
            router.Post("/debug-actions/run", context =>
                DebugActionHttpResponse.WriteAsync(context, service.Run(context.Request.QueryString["id"])));
            var saves = new GameSaveService();
            router.Post("/save", context =>
                DebugActionHttpResponse.WriteAsync(context, saves.Save(context.Request.QueryString["name"])));
        }
    }
}
