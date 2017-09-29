using Microsoft.AspNetCore.Authentication;

namespace MarginTrading.DataReader.Middleware
{
    public class KeyAuthOptions : AuthenticationSchemeOptions
    {
        public const string DefaultHeaderName = "api-key";
        public const string AuthenticationScheme = "Automatic";
    }
}
