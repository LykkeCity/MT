// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Exceptions;
using MarginTrading.Backend.Extensions;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Helpers;
using MarginTrading.Common.Settings;
using Microsoft.AspNetCore.Http;

namespace MarginTrading.Backend.Middleware
{
    public class ValidationExceptionHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILog _log;
        private readonly RequestLoggerSettings _settings;

        public ValidationExceptionHandler(IHttpContextAccessor httpContextAccessor,
            ILog log,
            RequestLoggerSettings settings)
        {
            _httpContextAccessor = httpContextAccessor;
            _log = log;
            _settings = settings;
        }
        
        public static bool CanHandle(Exception ex)
        {
            return (ex is ValidateOrderException orderException && orderException.IsPublic()) ||
                   ex is AccountValidationException ||
                   ex is InstrumentValidationException ||
                   ex is PositionValidationException;
        }

        /// <summary>
        /// Writes error to response in problem details format for only specific cases:
        /// <a href="https://tools.ietf.org/html/rfc7807">RFC 7807</a>.
        /// Check with <see cref="CanHandle"/> before calling this method.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public Task WriteProblemDetails(Exception ex)
        {
            switch (ex)
            {
                case ValidateOrderException e:
                    var publicErrorCode = PublicErrorCodeMap.Map(e.RejectReason);
                    return HandleOrderValidationException(e, publicErrorCode);
                case AccountValidationException e:
                    return HandleAccountValidationException(e);
                case InstrumentValidationException e:
                    return HandleInstrumentValidationException(e);
                case PositionValidationException e:
                    return HandlePositionValidationException(e);
            }
            
            return Task.CompletedTask;
        }

        private async Task HandlePositionValidationException(PositionValidationException ex)
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return;
            }
            
            await Log(ex);

            var responseErrorCode = PublicErrorCodeMap.Map(ex.ErrorCode); 

            var problemDetails = ProblemDetailsFactory.Create422(
                _httpContextAccessor.HttpContext.Request.Path,
                responseErrorCode,
                ex.Message);
            
            await _httpContextAccessor.HttpContext.Response.WriteProblemDetailsAsync(problemDetails);
        }

        private async Task HandleOrderValidationException(ValidateOrderException ex, string publicErrorCode)
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return;
            }
            
            await Log(ex);
            
            var problemDetails = ProblemDetailsFactory.Create422(
                _httpContextAccessor.HttpContext.Request.Path,
                publicErrorCode,
                ex.Message);
                
            await _httpContextAccessor.HttpContext.Response.WriteProblemDetailsAsync(problemDetails);
        }

        private async Task HandleAccountValidationException(AccountValidationException ex)
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return;
            }
            
            await Log(ex);

            var responseErrorCode = PublicErrorCodeMap.Map(ex.ErrorCode); 

            var problemDetails = ProblemDetailsFactory.Create422(
                _httpContextAccessor.HttpContext.Request.Path,
                responseErrorCode,
                ex.Message);
            
            await _httpContextAccessor.HttpContext.Response.WriteProblemDetailsAsync(problemDetails);
        }

        private async Task HandleInstrumentValidationException(InstrumentValidationException ex)
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return;
            }
            
            await Log(ex);

            var responseErrorCode = PublicErrorCodeMap.Map(ex.ErrorCode);

            var problemDetails = ProblemDetailsFactory.Create422(
                _httpContextAccessor.HttpContext.Request.Path,
                responseErrorCode,
                ex.Message);
            
            await _httpContextAccessor.HttpContext.Response.WriteProblemDetailsAsync(problemDetails);
        }
        
        private async Task Log(Exception ex)
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return;
            }

            var bytes = await _httpContextAccessor.HttpContext.Request.Body.ReadBytes(_settings.MaxPartSize);
            var bodyPart = bytes == null ? null : System.Text.Encoding.UTF8.GetString(bytes);

            var requestUri = _httpContextAccessor.HttpContext.Request.GetUri().AbsoluteUri;

            await _log.WriteInfoAsync(nameof(ValidationExceptionHandler), requestUri, bodyPart + ex.Message);
        }
    }
}