// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Exceptions;
using MarginTrading.Backend.Extensions;
using MarginTrading.Common.Helpers;
using Microsoft.AspNetCore.Http;

namespace MarginTrading.Backend.Middleware
{
    public class GlobalErrorHandlerMiddleware
    {
        private readonly ILog _log;
        private readonly RequestDelegate _next;
        private readonly ValidationExceptionHandler _validationExceptionHandler;

        public GlobalErrorHandlerMiddleware(RequestDelegate next, ILog log, ValidationExceptionHandler validationExceptionHandler)
        {
            _log = log;
            _validationExceptionHandler = validationExceptionHandler;
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
                var handled = await _validationExceptionHandler.TryHandleAsync(ex);

                if (handled) return;
                
                await Log(ex, asInfo: ex is LogInfoOnlyException);

                await context.Response.WriteDefaultMtErrorAsync(ex.Message);
            }
        }
        
        private async Task Log(Exception ex, bool asInfo)
        {
            string bodyPart;

            using (var ms = new MemoryStream())
            {
                bodyPart = await StreamHelpers.GetStreamPart(ms, 1024);
            }

            if (asInfo)
            {
                await _log.WriteInfoAsync(nameof(GlobalErrorHandlerMiddleware), bodyPart, ex.Message);
                return;
            }

            await _log.WriteErrorAsync(nameof(GlobalErrorHandlerMiddleware), bodyPart, ex);
        }
    }
}