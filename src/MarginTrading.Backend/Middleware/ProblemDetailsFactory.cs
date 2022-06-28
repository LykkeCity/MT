// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Refit;

namespace MarginTrading.Backend.Middleware
{
    public static class ProblemDetailsFactory
    {
        public static ProblemDetails Create(string title, string detail, string type, string instance, int status, Dictionary<string, string[]> errors)
        {
            return new ProblemDetails
            {
                Title = title,
                Detail = detail,
                Type = type,
                Instance = instance,
                Status = status,
                Errors = errors
            };
        }

        public static ProblemDetails Create(string requestPath, string errorCode, string errorMessage)
        {
            return Create("Internal Server Error",
                string.Empty,
                "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                requestPath,
                500,
                new Dictionary<string, string[]> { { errorCode, new[] { errorMessage } } });
        }
    }
}