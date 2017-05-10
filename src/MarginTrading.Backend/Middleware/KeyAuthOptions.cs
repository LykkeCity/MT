using Microsoft.AspNetCore.Builder;

#pragma warning disable 1591

namespace MarginTrading.Backend.Middleware
{
    public class KeyAuthOptions : AuthenticationOptions
    {
        public const string DefaultHeaderName = "api-key";
        public string KeyHeaderName { get; set; } = DefaultHeaderName;

        public KeyAuthOptions()
        {
            AuthenticationScheme = "Automatic";
        }
    }
}
