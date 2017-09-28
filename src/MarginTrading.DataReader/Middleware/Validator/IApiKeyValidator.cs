
using MarginTrading.DataReader.Settings;

namespace MarginTrading.DataReader.Middleware.Validator
{
    public interface IApiKeyValidator
    {
        bool Validate(string apiKey);
    }

    public class ApiKeyValidator : IApiKeyValidator
    {
        private readonly DataReaderSettings _settings;

        public ApiKeyValidator(DataReaderSettings settings)
        {
            _settings = settings;
        }

        public bool Validate(string apiKey)
        {
            return apiKey == _settings.ApiKey;
        }
    }
}
