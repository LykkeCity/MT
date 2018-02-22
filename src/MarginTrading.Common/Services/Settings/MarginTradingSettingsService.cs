using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client;
using Rocks.Caching;

namespace MarginTrading.Common.Services.Settings
{
    /// <summary>
    /// Detects if margin trading of particular types (live and demo) is available globally and for user.
    /// </summary>
    public class MarginTradingSettingsService : IMarginTradingSettingsService
    {
        private static readonly CachingParameters ClientTradingEnabledCachingParameters = CachingParameters.FromMinutes(1);

        private readonly IClientAccountClient _clientAccountClient;
        private readonly ICacheProvider _cacheProvider;

        public MarginTradingSettingsService(ICacheProvider cacheProvider, 
            IClientAccountClient clientAccountClient)
        {
            _cacheProvider = cacheProvider;
            _clientAccountClient = clientAccountClient;
        }

        public async Task<EnabledMarginTradingTypes> IsMarginTradingEnabled(string clientId)
            => await IsMarginTradingEnabledInternal(clientId);

        public async Task<bool> IsMarginTradingEnabled(string clientId, bool isLive)
        {
            var enabledTypes = await IsMarginTradingEnabled(clientId);
            return isLive ? enabledTypes.Live : enabledTypes.Demo;
        }

        public async Task SetMarginTradingEnabled(string clientId, bool isLive, bool enabled)
        {
            var settings = await _clientAccountClient.GetMarginEnabledAsync(clientId);

            if (isLive)
            {
                settings.EnabledLive = enabled;
            }
            else
            {
                settings.Enabled = enabled;
            }

            await _clientAccountClient.SetMarginEnabledAsync(clientId, settings.Enabled, settings.EnabledLive,
                settings.TermsOfUseAgreed);
            
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
                var marginEnabledSettings = await _clientAccountClient.GetMarginEnabledAsync(clientId);
                return new EnabledMarginTradingTypes { Demo = marginEnabledSettings.Enabled, Live = marginEnabledSettings.EnabledLive };
            }

            return _cacheProvider.GetAsync(GetClientTradingEnabledCacheKey(clientId), async () => new CachableResult<EnabledMarginTradingTypes>(await MarginEnabled(), ClientTradingEnabledCachingParameters));
        }

        private string GetClientTradingEnabledCacheKey(string clientId) => CacheKeyBuilder.Create(nameof(MarginTradingSettingsService), nameof(GetClientTradingEnabledCacheKey), clientId);
    }
}
