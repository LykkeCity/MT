using System.Threading.Tasks;
using MarginTrading.Common.Settings.Models;
using MarginTrading.Common.Settings.Repositories;
using MarginTrading.DataReader.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/settings")]
    public class SettingsController : Controller
    {
        private readonly Settings.DataReaderSettings _dataReaderSettings;
        private readonly IClientSettingsRepository _clientSettingsRepository;

        public SettingsController(Settings.DataReaderSettings dataReaderSettings, IClientSettingsRepository clientSettingsRepository)
        {
            _dataReaderSettings = dataReaderSettings;
            _clientSettingsRepository = clientSettingsRepository;
        }

        [HttpGet]
        [Route("enabled/{clientId}")]
        [SkipMarginTradingEnabledCheck]
        public async Task<bool> GetIsMarginTradingEnabled(string clientId)
        {
            var settings = await _clientSettingsRepository.GetSettings<MarginEnabledSettings>(clientId);
            return _dataReaderSettings.IsLive
                ? settings.EnabledLive
                : settings.Enabled;
        }
    }
}