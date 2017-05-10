using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MarginTrading.Backend.Middleware.Validator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;

#pragma warning disable 1591

namespace MarginTrading.Backend.Middleware
{
    public class KeyAuthHandler : AuthenticationHandler<KeyAuthOptions>
    {
        private readonly IApiKeyValidator _apiKeyValidator;

        public KeyAuthHandler(IApiKeyValidator apiKeyValidator)
        {
            _apiKeyValidator = apiKeyValidator;
        }
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            StringValues headerValue;

            if (!Context.Request.Headers.TryGetValue(Options.KeyHeaderName, out headerValue))
            {
                return AuthenticateResult.Fail("No api key header.");
            }

            var apiKey = headerValue.First();
            if (!_apiKeyValidator.Validate(apiKey))
            {
                return AuthenticateResult.Fail("Invalid API key.");
            }

            var identity = new ClaimsIdentity("apikey"); 
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), null, "apikey");
            return await Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
