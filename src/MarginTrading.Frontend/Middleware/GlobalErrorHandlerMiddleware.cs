using System;
using System.IO;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Common.Extensions;
using MarginTrading.Frontend.Models;
using MarginTrading.Frontend.Services;
using Microsoft.AspNetCore.Http;

namespace MarginTrading.Frontend.Middleware
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
            catch (MaintenanceException)
            {
                await SendMaintenanceError(context);
            }
            catch (Exception ex)
            {
                await LogError(context, ex);

                await SendError(context);
            }
        }

        private async Task LogError(HttpContext context, Exception ex)
        {
            using (var ms = new MemoryStream())
            {
                context.Request.Body.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                await _log.LogPartFromStream(ms, "GlobalHandler", context.Request.GetUri().AbsoluteUri, ex);
            }
        }

        private async Task SendError(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = 500;
            var response = ResponseModel.CreateFail(ResponseModel.ErrorCodeType.RuntimeProblem, "Technical problems");
            await ctx.Response.WriteAsync(response.ToJson());
        }
        
        private async Task SendMaintenanceError(HttpContext ctx)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = 503;
            var response = ResponseModel.CreateFail(ResponseModel.ErrorCodeType.MaintananceMode,
                "Sorry, application is on maintenance. Please try again later.");
            await ctx.Response.WriteAsync(response.ToJson());
        }
    }
}