

using MarginTrading.Backend.Core.Settings;

#pragma warning disable 1591

namespace MarginTrading.Backend.Middleware.Validator
{
    public interface IApiKeyValidator
    {
        bool Validate(string apiKey);
    }

    public class ApiKeyValidator : IApiKeyValidator
    {
        private readonly MarginTradingSettings _settings;

        public ApiKeyValidator(MarginTradingSettings settings)
        {
            _settings = settings;
        }

        public bool Validate(string apiKey)
        {
            return apiKey == _settings.ApiKey;
        }
    }
}
