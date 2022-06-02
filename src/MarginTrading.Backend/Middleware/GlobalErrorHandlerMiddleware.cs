// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Exceptions;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Helpers;
using MarginTrading.Contract.BackendContracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Refit;

namespace MarginTrading.Backend.Middleware
{
    public class GlobalErrorHandlerMiddleware
    {
        private readonly ILog _log;
        private readonly RequestDelegate _next;
        private readonly IHostingEnvironment _environment;

        public GlobalErrorHandlerMiddleware(RequestDelegate next, ILog log, IHostingEnvironment environment)
        {
            _log = log;
            _environment = environment;
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
                if (ex is ValidateOrderException || ex is LogInfoOnlyException)
                {
                    await LogValidationError(context, ex);
                }
                else
                {
                    await LogError(context, ex);
                }

                await SendException(context, ex);
            }
        }

        private async Task LogValidationError(HttpContext context, Exception ex)
        {
            string bodyPart;

            using (var memoryStream = new MemoryStream())
            {
                bodyPart = await StreamHelpers.GetStreamPart(memoryStream, 1024);
            }

            await _log.WriteInfoAsync("GlobalHandler", context.Request.GetUri().AbsoluteUri, bodyPart + ex.Message);
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

        private static async Task SendError(HttpContext ctx, string errorMessage)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = 500;
            var response = new MtBackendResponse<string> {Result = "Technical problems", ErrorMessage = errorMessage};
            await ctx.Response.WriteAsync(response.ToJson());
        }

        private async Task SendException(HttpContext ctx, Exception ex)
        {
            if (ex is ValidateOrderException validateOrderException)
            {
                var problemDetails = new ProblemDetails
                {
                    Title = "Order validation error",
                    Status = 500,
                    Detail = $"Error comments: {validateOrderException.Comment}.",
                    Errors = new Dictionary<string, string[]>
                    {
                        { validateOrderException.RejectReason.ToString(), new[] { validateOrderException.Message } }
                    },
                    Instance =
                        $"urn:{_environment.ApplicationName.ToLowerInvariant()}:{ctx.TraceIdentifier}:{new Guid().ToString("N")}"
                };

                ctx.Response.ContentType = "application/problem+json";
                ctx.Response.StatusCode = problemDetails.Status;
                await ctx.Response.WriteAsync(problemDetails.ToJson());
            }
            else if (ex is ValidationException<AccountValidationError> accountValidationException)
            {
                var problemDetails = new ProblemDetails
                {
                    Title = "Account validation error",
                    Status = 500,
                    Errors = new Dictionary<string, string[]>
                    {
                        { accountValidationException.ErrorCode.ToString(), new[] { accountValidationException.Message } }
                    },
                    Instance =
                        $"urn:{_environment.ApplicationName.ToLowerInvariant()}:{ctx.TraceIdentifier}:{Guid.NewGuid().ToString("N")}"
                };

                ctx.Response.ContentType = "application/problem+json";
                ctx.Response.StatusCode = problemDetails.Status;
                await ctx.Response.WriteAsync(problemDetails.ToJson());
            }
            else if (ex is ValidationException<InstrumentValidationError> instrumentValidationError)
            {
                var problemDetails = new ProblemDetails
                {
                    Title = "Instrument validation error",
                    Status = 500,
                    Errors = new Dictionary<string, string[]>
                    {
                        { instrumentValidationError.ErrorCode.ToString(), new[] { instrumentValidationError.Message } }
                    },
                    Instance =
                        $"urn:{_environment.ApplicationName.ToLowerInvariant()}:{ctx.TraceIdentifier}:{Guid.NewGuid().ToString("N")}"
                };

                ctx.Response.ContentType = "application/problem+json";
                ctx.Response.StatusCode = problemDetails.Status;
                await ctx.Response.WriteAsync(problemDetails.ToJson());
            }
            else SendError(ctx, ex.Message);
        }
    }
}