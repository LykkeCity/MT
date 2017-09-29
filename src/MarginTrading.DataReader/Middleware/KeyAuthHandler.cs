using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using MarginTrading.DataReader.Middleware.Validator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarginTrading.DataReader.Middleware
{
    public class KeyAuthHandler : AuthenticationHandler<KeyAuthOptions>
    {
        private readonly IApiKeyValidator _apiKeyValidator;

        public KeyAuthHandler(IOptionsMonitor<KeyAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IApiKeyValidator apiKeyValidator)
            : base(options, logger, encoder, clock)
        {
            _apiKeyValidator = apiKeyValidator;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Context.Request.Headers.TryGetValue(KeyAuthOptions.DefaultHeaderName, out var headerValue))
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
