using System.Threading.Tasks;
using MarginTrading.Core.Clients;
using MarginTrading.Core.Settings;
using MarginTrading.DataReader.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/settings")]
    public class SettingsController : Controller
    {
        private readonly Settings.MarginSettings _marginSettings;
        private readonly IClientSettingsRepository _clientSettingsRepository;

        public SettingsController(Settings.MarginSettings marginSettings, IClientSettingsRepository clientSettingsRepository)
        {
            _marginSettings = marginSettings;
            _clientSettingsRepository = clientSettingsRepository;
        }

        [HttpGet]
        [Route("enabled/{clientId}")]
        [SkipMarginTradingEnabledCheck]
        public async Task<bool> GetIsMarginTradingEnabled(string clientId)
        {
            var settings = await _clientSettingsRepository.GetSettings<MarginEnabledSettings>(clientId);
            return _marginSettings.IsLive
                ? settings.EnabledLive
                : settings.Enabled;
        }
    }
}