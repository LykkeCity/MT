using System;
using System.IO;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Helpers;
using MarginTrading.Contract.BackendContracts;
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
#if DEBUG
                await SendError(context, ex.ToString());
#else
                await SendError(context, ex.Message);
#endif
            }
        }

        private async Task LogError(HttpContext context, Exception ex)
        {
            string bodyPart;

            using (var ms = new MemoryStream())
            {
                bodyPart = await StreamHelpers.GetStreamPart(ms, 1024);
            }

            await _log.WriteErrorAsync("GlobalHandler", context.Request.GetUri().AbsoluteUri, bodyPart, ex);
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