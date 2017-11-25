using Microsoft.Extensions.Configuration;

namespace MarginTrading.Common.Extensions
{
    public static class ConfigurationRootExtensions
    {
        public static bool IsLive(this IConfigurationRoot configuration)
        {
            return !string.IsNullOrEmpty(configuration["IsLive"]) &&
                   bool.TryParse(configuration["IsLive"], out var isLive) && isLive;
        }
        
        public static string ServerType(this IConfigurationRoot configuration)
        {
            return configuration.IsLive() ? "Live" : "Demo";
        }
    }
}
