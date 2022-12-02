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
            return (ex is OrderRejectionException orderException && orderException.IsPublic()) ||
                   ex is AccountValidationException ||
                   ex is InstrumentValidationException ||
                   ex is PositionValidationException ||
                   ex is OrderValidationException ||
                   ex is PositionGroupValidationException;
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
                case OrderRejectionException e:
                    return HandleOrderRejectionException(e);
                case AccountValidationException e:
                    return HandleValidationException(e, PublicErrorCodeMap.Map);
                case InstrumentValidationException e:
                    return HandleValidationException(e, PublicErrorCodeMap.Map);
                case PositionValidationException e:
                    return HandleValidationException(e, PublicErrorCodeMap.Map);
                case OrderValidationException e:
                    return HandleValidationException(e, PublicErrorCodeMap.Map);
                case PositionGroupValidationException e:
                    return HandleValidationException(e, PublicErrorCodeMap.Map);
            }
            
            return Task.CompletedTask;
        }
        
        private async Task HandleOrderRejectionException(OrderRejectionException ex)
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return;
            }
            
            await Log(ex);

            var responseErrorCode = PublicErrorCodeMap.Map(ex.RejectReason);
            
            var problemDetails = ProblemDetailsFactory.Create422(
                _httpContextAccessor.HttpContext.Request.Path,
                responseErrorCode,
                ex.Message);
                
            await _httpContextAccessor.HttpContext.Response.WriteProblemDetailsAsync(problemDetails);
        }

        private async Task HandleValidationException<T>(ValidationException<T> ex, Func<T, string> errorCodeMapper)
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                return;
            }
            
            await Log(ex);

            var responseErrorCode = errorCodeMapper?.Invoke(ex.ErrorCode) ?? string.Empty;

            var problemDetails = ProblemDetailsFactory.Create400(
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