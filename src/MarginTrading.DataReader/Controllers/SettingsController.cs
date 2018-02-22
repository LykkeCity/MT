using System.Threading.Tasks;
using MarginTrading.Common.Services.Settings;
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
        private readonly IMarginTradingSettingsService _marginTradingSettingsService;

        public SettingsController(Settings.DataReaderSettings dataReaderSettings, 
            IMarginTradingSettingsService marginTradingSettingsService)
        {
            _dataReaderSettings = dataReaderSettings;
            _marginTradingSettingsService = marginTradingSettingsService;
        }

        [HttpGet]
        [Route("enabled/{clientId}")]
        [SkipMarginTradingEnabledCheck]
        public async Task<bool> GetIsMarginTradingEnabled(string clientId)
        {
            var settings = await _marginTradingSettingsService.IsMarginTradingEnabled(clientId);
            return _dataReaderSettings.IsLive
                ? settings.Live
                : settings.Demo;
        }
    }
}