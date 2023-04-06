// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Common.Helpers;
using MarginTrading.Common.Services;
using MarginTrading.Common.Settings;
using Microsoft.AspNetCore.Http;

namespace MarginTrading.Common.Middleware
{
    [UsedImplicitly]
    public class RequestsLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RequestLoggerSettings _settings;
        private readonly ILog _log;
        private readonly ILog _requestsLog;

        private const int MaxStorageFieldLength = 2000;
        private readonly string[] _personalDataHeaders = {"Authorization", "api-key"};

        public RequestsLoggingMiddleware(RequestDelegate next, RequestLoggerSettings settings, ILog log)
        {
            _next = next;
            _settings = settings;
            _log = log;
            _requestsLog = LogLocator.RequestsLog;
        }

        [UsedImplicitly]
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
                    reqBodyStream.Seek(0, SeekOrigin.Begin);
                    await reqBodyStream.CopyToAsync(originalRequestBody);
                    reqBodyStream.Seek(0, SeekOrigin.Begin);
                    context.Request.Body = reqBodyStream;

                    using (originalRequestBody)
                    {
                        var bytes = await originalRequestBody.ReadBytes((uint)_settings.MaxPartSize);
                        var body = bytes == null ? null : System.Text.Encoding.UTF8.GetString(bytes);
                        var headers = context.Request.Headers.Where(h => !_personalDataHeaders.Contains(h.Key)).ToJson();
                        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        var info =
                            $"UserId:{userId} {Environment.NewLine}Body:{body} {Environment.NewLine}Headers:{headers}";
                        if (info.Length > MaxStorageFieldLength)
                        {
                            info = info.Substring(0, MaxStorageFieldLength);
                        }

                        await _requestsLog.WriteInfoAsync("MIDDLEWARE", "RequestsLoggingMiddleware", requestContext,
                            info);
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
