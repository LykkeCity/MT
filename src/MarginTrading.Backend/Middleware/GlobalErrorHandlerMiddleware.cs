using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Common.Extensions;
using MarginTrading.Core.Notifications;
using MarginTrading.Core.Settings;
using Microsoft.AspNetCore.Http;

namespace MarginTrading.Backend.Middleware
{
    public class GlobalErrorHandlerMiddleware
    {
        private readonly ILog _log;
        private readonly RequestDelegate _next;

        public GlobalErrorHandlerMiddleware(RequestDelegate next, ILog log)
        {
            _log = log;
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                await LogError(context, ex);

                await SendError(context, ex.Message);
            }
        }

        private async Task LogError(HttpContext context, Exception ex)
        {
            string bodyPart;

            using (var ms = new MemoryStream())
            {
                context.Request.Body.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                bodyPart = await GetBodyPart(ms);
            }

            await _log.WriteErrorAsync("GlobalHandler", context.Request.GetUri().AbsoluteUri, bodyPart, ex);
        }

        private const int PartSize = 1024;
        private async Task<string> GetBodyPart(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            var requestReader = new StreamReader(stream);
            int len = (int)(stream.Length > PartSize ? PartSize : stream.Length);
            char[] bodyPart = new char[len];
            await requestReader.ReadAsync(bodyPart, 0, len);

            return new string(bodyPart);
        }

        private async Task SendError(HttpContext ctx, string errorMessage)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = 500;
            var response = new MtBackendResponse<string>() {Result = "Technical problems", Message = errorMessage};
            await ctx.Response.WriteAsync(response.ToJson());
        }
    }
}