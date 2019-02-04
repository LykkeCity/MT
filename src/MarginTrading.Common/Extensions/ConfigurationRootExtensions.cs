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
        
        public static bool NotThrowExceptionsOnServiceValidation(this IConfigurationRoot configuration)
        {
            return !string.IsNullOrEmpty(configuration["NOT_THROW_EXCEPTIONS_ON_SERVICES_VALIDATION"]) &&
                   bool.TryParse(configuration["NOT_THROW_EXCEPTIONS_ON_SERVICES_VALIDATION"],
                       out var trowExceptionsOnInvalidService) && trowExceptionsOnInvalidService;
        }
        
        public static string ServerType(this IConfigurationRoot configuration)
        {
            return configuration.IsLive() ? "Live" : "Demo";
        }
    }
}
