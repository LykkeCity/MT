using System.Text.Encodings.Web;
using MarginTrading.Backend.Middleware.Validator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

#pragma warning disable 1591

namespace MarginTrading.Backend.Middleware
{
    public class KeyAuthMiddleware : AuthenticationMiddleware<KeyAuthOptions>
    {
        private readonly IApiKeyValidator _apiKeyValidator;

        public KeyAuthMiddleware(
            IApiKeyValidator apiKeyValidator,
            RequestDelegate next, 
            IOptions<KeyAuthOptions> options, 
            ILoggerFactory loggerFactory, 
            UrlEncoder encoder) : base(next, options, loggerFactory, encoder)
        {
            _apiKeyValidator = apiKeyValidator;
        }

        protected override AuthenticationHandler<KeyAuthOptions> CreateHandler()
        {
            return new KeyAuthHandler(_apiKeyValidator);
        }
    }
}
