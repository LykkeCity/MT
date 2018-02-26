using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client;
using MarginTrading.Backend.Contracts.RabbitMqMessages;
using Rocks.Caching;

namespace MarginTrading.Common.Services.Settings
{
    /// <summary>
    /// Detects if margin trading of particular types (live and demo) is available globally and for user.
    /// </summary>
    public class MarginTradingEnabledCacheService : IMarginTradingSettingsCacheService
    {
        private static readonly CachingParameters ClientTradingEnabledCachingParameters =
            CachingParameters.InfiniteCache;

        private readonly IClientAccountClient _clientAccountClient;
        private readonly ICacheProvider _cacheProvider;

        public MarginTradingEnabledCacheService(ICacheProvider cacheProvider,
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

        public void OnMarginTradingEnabledChanged(MarginTradingEnabledChangedMessage message)
        {
            _cacheProvider.Add(GetClientTradingEnabledCacheKey(message.ClientId),
                new EnabledMarginTradingTypes {Demo = message.EnabledDemo, Live = message.EnabledLive},
                ClientTradingEnabledCachingParameters);
        }

        private Task<EnabledMarginTradingTypes> IsMarginTradingEnabledInternal(string clientId)
        {
            async Task<EnabledMarginTradingTypes> MarginEnabled()
            {
                var marginEnabledSettings = await _clientAccountClient.GetMarginEnabledAsync(clientId);
                return new EnabledMarginTradingTypes
                {
                    Demo = marginEnabledSettings.Enabled,
                    Live = marginEnabledSettings.EnabledLive
                };
            }

            return _cacheProvider.GetAsync(GetClientTradingEnabledCacheKey(clientId),
                async () => new CachableResult<EnabledMarginTradingTypes>(await MarginEnabled(),
                    ClientTradingEnabledCachingParameters));
        }

        private string GetClientTradingEnabledCacheKey(string clientId) =>
            CacheKeyBuilder.Create(nameof(MarginTradingEnabledCacheService), nameof(GetClientTradingEnabledCacheKey),
                clientId);
    }
}