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

        public async Task<bool> TryHandleAsync(Exception ex)
        {
            if (ex is ValidateOrderException validateOrderException)
            {
                return await TryHandleOrderValidationException(validateOrderException);
            }

            if (ex is AccountValidationException accountValidationException)
            {
                await HandleAccountValidationException(accountValidationException);
                return true;
            }

            if (ex is InstrumentValidationException instrumentValidationException)
            {
                await HandleInstrumentValidationException(instrumentValidationException);
                return true;
            }

            return false;
        }

        private async Task<bool> TryHandleOrderValidationException(ValidateOrderException ex)
        {
            var responseErrorCode = ResponseErrorCodeMap.MapOrderRejectReason(ex.RejectReason);

            if (responseErrorCode == ResponseErrorCodeMap.UnsupportedError)
            {
                return false;
            }

            await Log(ex);
                
            var problemDetails = ProblemDetailsFactory.Create(
                _httpContextAccessor.HttpContext.Request.Path,
                responseErrorCode,
                ex.Message);
                
            await _httpContextAccessor.HttpContext.Response.WriteProblemDetailsAsync(problemDetails);
            
            return true;
        }

        private async Task HandleAccountValidationException(AccountValidationException ex)
        {
            await Log(ex);

            var responseErrorCode = ResponseErrorCodeMap.MapAccountValidationError(ex.ErrorCode); 

            var problemDetails = ProblemDetailsFactory.Create(
                _httpContextAccessor.HttpContext.Request.Path,
                responseErrorCode,
                ex.Message);
            
            await _httpContextAccessor.HttpContext.Response.WriteProblemDetailsAsync(problemDetails);
        }

        private async Task HandleInstrumentValidationException(InstrumentValidationException ex)
        {
            await Log(ex);

            var responseErrorCode = ResponseErrorCodeMap.MapInstrumentValidationError(ex.ErrorCode);

            var problemDetails = ProblemDetailsFactory.Create(
                _httpContextAccessor.HttpContext.Request.Path,
                responseErrorCode,
                ex.Message);
            
            await _httpContextAccessor.HttpContext.Response.WriteProblemDetailsAsync(problemDetails);
        }
        
        private async Task Log(Exception ex)
        {
            var bytes = await _httpContextAccessor.HttpContext.Request.Body.ReadBytes(_settings.MaxPartSize);
            string bodyPart = bytes == null ? null : System.Text.Encoding.UTF8.GetString(bytes);

            var requestUri = _httpContextAccessor.HttpContext.Request.GetUri().AbsoluteUri;

            await _log.WriteInfoAsync(nameof(ValidationExceptionHandler), requestUri, bodyPart + ex.Message);
        }
    }
}