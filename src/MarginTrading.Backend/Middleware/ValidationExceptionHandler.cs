// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Exceptions;
using MarginTrading.Backend.Extensions;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Helpers;
using Microsoft.AspNetCore.Http;

namespace MarginTrading.Backend.Middleware
{
    public class ValidationExceptionHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILog _log;

        public ValidationExceptionHandler(IHttpContextAccessor httpContextAccessor, ILog log)
        {
            _httpContextAccessor = httpContextAccessor;
            _log = log;
        }
        
        public static bool CanHandle(Exception ex)
        {
            return (ex is ValidateOrderException orderException && orderException.IsPublic()) ||
                   ex is AccountValidationException ||
                   ex is InstrumentValidationException;
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
            }
            
            return Task.CompletedTask;
        }
        
        private async Task HandleOrderValidationException(ValidateOrderException ex, string publicErrorCode)
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return;
            }
            
            await Log(ex);
            
            var problemDetails = ProblemDetailsFactory.Create(
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

            var problemDetails = ProblemDetailsFactory.Create(
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

            var problemDetails = ProblemDetailsFactory.Create(
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
            
            string bodyPart;

            using (var memoryStream = new MemoryStream())
            {
                bodyPart = await StreamHelpers.GetStreamPart(memoryStream, 1024);
            }

            var requestUri = _httpContextAccessor.HttpContext.Request.GetUri().AbsoluteUri;

            await _log.WriteInfoAsync(nameof(ValidationExceptionHandler), requestUri, bodyPart + ex.Message);
        }
    }
}