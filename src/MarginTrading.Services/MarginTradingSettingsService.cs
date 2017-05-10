using System.Threading.Tasks;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Core.Settings;

namespace MarginTrading.Services
{
    public class MarginTradingSettingsService : IMarginTradingSettingsService
    {
        private readonly IClientSettingsRepository _clientSettingsRepository;
        private readonly IAppGlobalSettingsRepositry _appGlobalSettingsRepositry;

        public MarginTradingSettingsService(IClientSettingsRepository clientSettingsRepository,
            IAppGlobalSettingsRepositry appGlobalSettingsRepositry)
        {
            _clientSettingsRepository = clientSettingsRepository;
            _appGlobalSettingsRepositry = appGlobalSettingsRepositry;
        }

        public async Task<bool> IsMargingTradingEnabled(string clientId)
        {
            return (await _appGlobalSettingsRepositry.GetAsync()).MarginTradingEnabled &&
                   (await _clientSettingsRepository.GetSettings<MarginEnabledSettings>(clientId)).Enabled;
        }
    }
}
