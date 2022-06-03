// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Common;
using MarginTrading.Contract.BackendContracts;
using Microsoft.AspNetCore.Http;
using Refit;

namespace MarginTrading.Backend.Extensions
{
    public static class HttpResponseExtensions
    {
        public static Task WriteProblemDetailsAsync(this HttpResponse response, ProblemDetails problemDetails)
        {
            response.ContentType = "application/problem+json";
            response.StatusCode = problemDetails.Status;
            return response.WriteAsync(problemDetails.ToJson());
        }

        public static Task WriteDefaultMtErrorAsync(this HttpResponse response, string errorMessage)
        {
            response.ContentType = "application/json";
            response.StatusCode = 500;
            var errorObject = new MtBackendResponse<string> {Result = "Technical problems", ErrorMessage = errorMessage};
            return response.WriteAsync(errorObject.ToJson());
        }
    }
}