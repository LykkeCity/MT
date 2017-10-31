

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
        private readonly MarginSettings _settings;

        public ApiKeyValidator(MarginSettings settings)
        {
            _settings = settings;
        }

        public bool Validate(string apiKey)
        {
            return apiKey == _settings.ApiKey;
        }
    }
}
