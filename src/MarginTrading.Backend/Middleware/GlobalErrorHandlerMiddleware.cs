// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Exceptions;
using MarginTrading.Backend.Extensions;
using MarginTrading.Common.Helpers;
using MarginTrading.Common.Settings;
using Microsoft.AspNetCore.Http;

namespace MarginTrading.Backend.Middleware
{
    public class GlobalErrorHandlerMiddleware
    {
        private readonly ILog _log;
        private readonly RequestDelegate _next;
        private readonly ValidationExceptionHandler _validationExceptionHandler;
        private readonly RequestLoggerSettings _settings;

        public GlobalErrorHandlerMiddleware(RequestDelegate next,
            ILog log,
            ValidationExceptionHandler validationExceptionHandler,
            RequestLoggerSettings settings)
        {
            _log = log;
            _validationExceptionHandler = validationExceptionHandler;
            _settings = settings;
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
                if (ValidationExceptionHandler.CanHandleException(ex))
                {
                    await _validationExceptionHandler.WriteProblemDetails(ex);
                    return;
                }

                await LogWithRequest(context.Request, ex, ex is LogInfoOnlyException);

                await context.Response.WriteDefaultMtErrorAsync(ex.Message);
            }
        }
        
        private async Task LogWithRequest(HttpRequest request, Exception ex, bool asInfo)
        {
            var bytes = await request.Body.ReadBytes(_settings.MaxPartSize);
            var bodyPart = bytes == null ? null : System.Text.Encoding.UTF8.GetString(bytes);

            if (asInfo)
            {
                await _log.WriteInfoAsync(nameof(GlobalErrorHandlerMiddleware), $"Body: {bodyPart}", ex.Message);
                return;
            }
            
            if (ex.InnerException != null)
            {
                await _log.WriteErrorAsync(nameof(GlobalErrorHandlerMiddleware), 
                    $"Body: {bodyPart}", 
                    ex.InnerException);
            }

            await _log.WriteErrorAsync(nameof(GlobalErrorHandlerMiddleware), $"Body: {bodyPart}", ex);
        }
    }
}