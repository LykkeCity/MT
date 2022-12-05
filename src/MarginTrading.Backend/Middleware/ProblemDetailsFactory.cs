// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Refit;

namespace MarginTrading.Backend.Middleware
{
    public static class ProblemDetailsFactory
    {
        private static ProblemDetails Create(string title,
            string detail,
            string type,
            string instance,
            int status,
            Dictionary<string, string[]> errors)
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

        public static ProblemDetails Create422(string requestPath, string errorCode, string errorMessage)
        {
            return Create("Unprocessable Entity",
                errorMessage,
                "https://www.rfc-editor.org/rfc/rfc4918#section-11.2",
                requestPath,
                422,
                new Dictionary<string, string[]> { { errorCode, new[] { errorMessage } } });
        }

        public static ProblemDetails Create400(string requestPath, string errorCode, string errorMessage)
        {
            return Create("Bad Request",
                string.Empty,
                "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                requestPath,
                400,
                new Dictionary<string, string[]> { { errorCode, new[] { errorMessage } } });
        }
    }
}