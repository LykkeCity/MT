using System;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Core.Settings;
using Rocks.Caching;

namespace MarginTrading.Services
{
    /// <summary>
    /// Detects if margin trading of specified type (live or not) is available globally and for user.
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

        public async Task<bool> IsMarginTradingDemoEnabled(string clientId)
            => await IsMarginTradingEnabled(clientId, false);

        public async Task<bool> IsMarginTradingLiveEnabled(string clientId)
            => await IsMarginTradingEnabled(clientId, true);

        public async Task<bool> IsMarginTradingEnabled(string clientId, bool isLive)
            => await IsMarginTradingEnabledGlobally() && await IsMarginTradingEnabledInternal(clientId, isLive);

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
            _cacheProvider.Add(GetClientTradingEnabledCacheKey(clientId, isLive), enabled, ClientTradingEnabledCachingParameters);
        }

        private Task<bool> IsMarginTradingEnabledInternal(string clientId, bool isLive)
        {
            var cacheKey = GetClientTradingEnabledCacheKey(clientId, isLive);
            async Task<bool> MarginEnabled() => GetMarginEnabledFromSettings(await _clientSettingsRepository.GetSettings<MarginEnabledSettings>(clientId), isLive);
            return _cacheProvider.GetAsync(cacheKey, async () => new CachableResult<bool>(await MarginEnabled(), ClientTradingEnabledCachingParameters));
        }

        private Task<bool> IsMarginTradingEnabledGlobally()
        {
            var cacheKey = CacheKeyBuilder.Create(nameof(MarginTradingSettingsService), nameof(IsMarginTradingEnabledGlobally));
            async Task<bool> MarginEnabled() => (await _appGlobalSettingsRepository.GetAsync()).MarginTradingEnabled;
            return _cacheProvider.GetAsync(cacheKey, async () => new CachableResult<bool>(await MarginEnabled(), CachingParameters.FromHours(1)));
        }

        private string GetClientTradingEnabledCacheKey(string clientId, bool isLive) => CacheKeyBuilder.Create(nameof(MarginTradingSettingsService), nameof(GetClientTradingEnabledCacheKey), isLive, clientId);

        private static bool GetMarginEnabledFromSettings(MarginEnabledSettings settings, bool isLive) => isLive ? settings.EnabledLive : settings.Enabled;
    }
}
