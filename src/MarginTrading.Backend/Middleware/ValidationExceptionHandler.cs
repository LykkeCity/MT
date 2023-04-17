// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.ErrorCodes;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Exceptions;
using MarginTrading.Backend.Extensions;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Helpers;
using MarginTrading.Common.Settings;
using Microsoft.AspNetCore.Http;

namespace MarginTrading.Backend.Middleware
{
    /// <summary>
    /// Handles exceptions - inheritors from <see cref="ValidationException{T}"/>
    /// and returns responses as RFC 7807 compliant Problem Details with
    /// corresponding business error code.
    /// To add new exception following steps are required:
    /// 1. Add new domain error code enum.
    /// 2. Add new corresponsing errors to be used as public error codes to
    /// <see cref="ValidationErrorCodes"/> class.
    /// 3. Add mapping from domain error code to public error code
    /// to <see cref="PublicErrorCodeMap"/> class.
    /// 4. Finally, add new exception class - inheritor
    /// from <see cref="ValidationException{T}"/>
    /// </summary>
    public class ValidationExceptionHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILog _log;
        private readonly RequestLoggerSettings _settings;

        public ValidationExceptionHandler(IHttpContextAccessor httpContextAccessor,
            ILog log,
            RequestLoggerSettings settings)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }
        
        public static bool CanHandleException(Exception ex)
        {
            return (ex is OrderRejectionException orderException && orderException.IsPublic()) ||
                   ex.IsValidationException();
        }

        /// <summary>
        /// Writes error to response in problem details format for only specific cases:
        /// <a href="https://tools.ietf.org/html/rfc7807">RFC 7807</a>.
        /// Check with <see cref="CanHandleException"/> before calling this method.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public Task WriteProblemDetails(Exception ex)
        {
            switch (ex)
            {
                case OrderRejectionException e:
                    return HandleOrderRejectionException(e);
                case { } e when e.IsValidationException():
                    var publicErrorCode = PublicErrorCodeMap.MapFromValidationExceptionOrRaise(e);
                    return HandleValidationException(e, publicErrorCode);
            }
            
            return Task.CompletedTask;
        }
        
        private async Task HandleOrderRejectionException(OrderRejectionException ex)
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return;
            }
            
            var responseErrorCode = PublicErrorCodeMap.Map(ex.RejectReason);
            
            await Log(ex, responseErrorCode);

            var problemDetails = ProblemDetailsFactory.Create422(
                _httpContextAccessor.HttpContext.Request.Path,
                responseErrorCode,
                ex.Message);
                
            await _httpContextAccessor.HttpContext.Response.WriteProblemDetailsAsync(problemDetails);
        }

        private async Task HandleValidationException(Exception ex, string responseErrorCode)
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return;
            }
            
            await Log(ex, responseErrorCode);

            var problemDetails = ProblemDetailsFactory.Create400(
                _httpContextAccessor.HttpContext.Request.Path,
                responseErrorCode,
                ex.Message);
            
            await _httpContextAccessor.HttpContext.Response.WriteProblemDetailsAsync(problemDetails);
        }
        
        private async Task Log(Exception ex, string responseErrorCode)
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return;
            }
            
            var bytes = await _httpContextAccessor.HttpContext.Request.Body.ReadBytes((uint)_settings.MaxPartSize);
            string bodyPart = bytes == null ? null : System.Text.Encoding.UTF8.GetString(bytes);

            var requestUri = _httpContextAccessor.HttpContext.Request.GetUri().AbsoluteUri;

            await _log.WriteWarningAsync(nameof(ValidationExceptionHandler),
                new { requestUri, responseErrorCode }.ToJson(),
                new { bodyPart, message = ex.Message }.ToJson());
        }
    }
}