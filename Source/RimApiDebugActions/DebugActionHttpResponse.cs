using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RimApiDebugActions
{
    internal static class DebugActionHttpResponse
    {
        public static async Task WriteAsync(HttpListenerContext context, object payload, int statusCode = 200)
        {
            var response = context.Response;
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";

            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
            response.ContentLength64 = bytes.Length;

            try
            {
                await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            finally
            {
                response.Close();
            }
        }
    }
}
