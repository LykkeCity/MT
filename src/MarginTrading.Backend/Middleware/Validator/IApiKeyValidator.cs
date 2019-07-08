// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
