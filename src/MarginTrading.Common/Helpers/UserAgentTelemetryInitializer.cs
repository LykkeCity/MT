// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
            const string propName = "UserAgent";
            if (string.IsNullOrEmpty(telemetry.Context.Properties.TryGetValue(propName, out var value) ? value : string.Empty))
            {
                telemetry.Context.Properties[propName] = _httpContextAccessor.HttpContext?.Request?.Headers[HeaderNames.UserAgent].ToString();
            }
        }
    }
}