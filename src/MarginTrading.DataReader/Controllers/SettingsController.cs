using System.Threading.Tasks;
using MarginTrading.Backend.Contracts;
using MarginTrading.Common.Services.Settings;
using MarginTrading.DataReader.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/settings")]
    public class SettingsController : Controller, ISettingsReadingApi
    {
        private readonly Settings.DataReaderSettings _dataReaderSettings;
        private readonly IMarginTradingSettingsCacheService _marginTradingSettingsCacheService;

        public SettingsController(Settings.DataReaderSettings dataReaderSettings, 
            IMarginTradingSettingsCacheService marginTradingSettingsCacheService)
        {
            _dataReaderSettings = dataReaderSettings;
            _marginTradingSettingsCacheService = marginTradingSettingsCacheService;
        }

        [HttpGet]
        [Route("enabled/{clientId}")]
        [SkipMarginTradingEnabledCheck]
        public Task<bool> IsMarginTradingEnabled(string clientId)
        {
            return _marginTradingSettingsCacheService.IsMarginTradingEnabled(clientId, _dataReaderSettings.IsLive);
        }
    }
}