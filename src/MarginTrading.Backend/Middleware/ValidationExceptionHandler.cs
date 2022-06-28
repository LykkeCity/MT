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