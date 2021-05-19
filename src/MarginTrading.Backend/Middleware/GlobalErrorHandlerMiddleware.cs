// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Exceptions;
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

        [UsedImplicitly]
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception ex)
            {
                if(ex is ValidateOrderFunctionalException)
                {
                    await LogFunctionalError(context);
                }
                else
                {
                    await LogError(context, ex);
                }

#if DEBUG
                await SendError(context, ex.ToString());
#else
                await SendError(context, ex.Message);
#endif
            }
        }

        private async Task LogFunctionalError(HttpContext context)
        {
            string bodyPart;

            using (var memoryStream = new MemoryStream())
            {
                bodyPart = await StreamHelpers.GetStreamPart(memoryStream, 1024);
            }

            await _log.WriteInfoAsync("GlobalHandler", context.Request.GetUri().AbsoluteUri, bodyPart);
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
            var response = new MtBackendResponse<string> {Result = "Technical problems", ErrorMessage = errorMessage};
            await ctx.Response.WriteAsync(response.ToJson());
        }
    }
}