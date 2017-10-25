using System;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Core.Models;
using MarginTrading.Core.Settings;
using Rocks.Caching;

namespace MarginTrading.Services
{
    /// <summary>
    /// Detects if margin trading of particular types (live and demo) is available globally and for user.
    /// </summary>
    public class MarginTradingSettingsService : IMarginTradingSettingsService
    {
        private static readonly CachingParameters ClientTradingEnabledCachingParameters = CachingParameters.FromMinutes(5);

        private readonly IClientSettingsRepository _clientSettingsRepository;
        private readonly IAppGlobalSettingsRepositry _appGlobalSettingsRepository;
        private readonly ICacheProvider _cacheProvider;

        public MarginTradingSettingsService(
            IClientSettingsRepository clientSettingsRepository,
            IAppGlobalSettingsRepositry appGlobalSettingsRepository,
            ICacheProvider cacheProvider)
        {
            _clientSettingsRepository = clientSettingsRepository;
            _appGlobalSettingsRepository = appGlobalSettingsRepository;
            _cacheProvider = cacheProvider;
        }

        public async Task<EnabledMarginTradingTypes> IsMarginTradingEnabled(string clientId)
            => await IsMarginTradingEnabledGlobally()
                   ? await IsMarginTradingEnabledInternal(clientId)
                   : new EnabledMarginTradingTypes { Demo = false, Live = false };

        public async Task<bool> IsMarginTradingEnabled(string clientId, bool isLive)
        {
            var enabledTypes = await IsMarginTradingEnabled(clientId);
            return isLive ? enabledTypes.Live : enabledTypes.Demo;
        }

        public async Task SetMarginTradingEnabled(string clientId, bool isLive, bool enabled)
        {
            var settings = await _clientSettingsRepository.GetSettings<MarginEnabledSettings>(clientId);

            if (isLive)
            {
                settings.EnabledLive = enabled;
            }
            else
            {
                settings.Enabled = enabled;
            }

            await _clientSettingsRepository.SetSettings(clientId, settings);
            _cacheProvider.Add(GetClientTradingEnabledCacheKey(clientId),
                               new EnabledMarginTradingTypes { Demo = settings.Enabled, Live = settings.EnabledLive },
                               ClientTradingEnabledCachingParameters);
        }

        public void ResetCacheForClient(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                return;
            
            _cacheProvider.Remove(GetClientTradingEnabledCacheKey(clientId));
        }

        private Task<EnabledMarginTradingTypes> IsMarginTradingEnabledInternal(string clientId)
        {
            async Task<EnabledMarginTradingTypes> MarginEnabled()
            {
                var marginEnabledSettings = await _clientSettingsRepository.GetSettings<MarginEnabledSettings>(clientId);
                return new EnabledMarginTradingTypes { Demo = marginEnabledSettings.Enabled, Live = marginEnabledSettings.EnabledLive };
            }

            return _cacheProvider.GetAsync(GetClientTradingEnabledCacheKey(clientId), async () => new CachableResult<EnabledMarginTradingTypes>(await MarginEnabled(), ClientTradingEnabledCachingParameters));
        }

        private Task<bool> IsMarginTradingEnabledGlobally()
        {
            var cacheKey = CacheKeyBuilder.Create(nameof(MarginTradingSettingsService), nameof(IsMarginTradingEnabledGlobally));
            async Task<bool> MarginEnabled() => (await _appGlobalSettingsRepository.GetAsync()).MarginTradingEnabled;
            return _cacheProvider.GetAsync(cacheKey, async () => new CachableResult<bool>(await MarginEnabled(), CachingParameters.FromHours(1)));
        }

        private string GetClientTradingEnabledCacheKey(string clientId) => CacheKeyBuilder.Create(nameof(MarginTradingSettingsService), nameof(GetClientTradingEnabledCacheKey), clientId);
    }
}
