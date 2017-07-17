using System;
using System.IO;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Core.Settings;
using MarginTrading.Services.Helpers;
using MarginTrading.Services.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace MarginTrading.Services.Middleware
{
    public class RequestsLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RequestLoggerSettings _settings;
        private readonly ILog _log;
        private readonly ILog _requestsLog;

        private const int MaxStorageFieldLength = 2000;

        public RequestsLoggingMiddleware(RequestDelegate next, RequestLoggerSettings settings, ILog log)
        {
            _next = next;
            _settings = settings;
            _log = log;
            _requestsLog = LogLocator.RequestsLog;
        }

        public async Task Invoke(HttpContext context)
        {
            var requestContext =
                $"Request path: {context?.Request?.Path}, {Environment.NewLine}Method: {context?.Request?.Method}";
            try
            {
                if (_settings.Enabled && context.Request.Method.ToUpper() != "GET")
                {
                    var reqBodyStream = new MemoryStream();
                    var originalRequestBody = new MemoryStream();

                    await context.Request.Body.CopyToAsync(reqBodyStream);
                    await context.Request.Body.CopyToAsync(originalRequestBody);
                    reqBodyStream.Seek(0, SeekOrigin.Begin);
                    context.Request.Body = reqBodyStream;

                    using (originalRequestBody)
                    {
                        var body = await StreamHelpers.GetStreamPart(originalRequestBody, _settings.MaxPartSize);
                        var info =
                            $"Body: {body} {Environment.NewLine}Headers:{context.Request.Headers.ToJson()}";
                        if (info.Length > MaxStorageFieldLength)
                        {
                            info = info.Substring(0, MaxStorageFieldLength);
                        }

                        await _requestsLog.WriteInfoAsync("MIDDLEWARE", "RequestsLoggingMiddleware", requestContext, info);
                    }
                }
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync("MIDDLEWARE", "RequestsLoggingMiddleware", requestContext, ex);
            }
            finally
            {
                await _next.Invoke(context);
            }
        }
    }
}
