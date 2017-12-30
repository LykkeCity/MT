using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace MarginTrading.Common.Helpers
{
    public class UserAgentTelemetryInitializer : ITelemetryInitializer
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserAgentTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.User.UserAgent))
            {
                telemetry.Context.User.UserAgent = _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.UserAgent];
            }
        }
    }
}