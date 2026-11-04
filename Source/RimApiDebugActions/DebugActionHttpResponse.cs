using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RIMAPI.Http;

namespace RimApiDebugActions
{
    internal static class DebugActionHttpResponse
    {
        public static async Task WriteAsync(RimApiContext context, object payload, int statusCode = 200)
        {
            var response = context.Response;
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";

            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
            await response.SendAsync(bytes);
        }
    }
}
