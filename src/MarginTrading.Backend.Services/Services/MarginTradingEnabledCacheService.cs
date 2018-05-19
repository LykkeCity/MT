using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Services.Client;
using MarginTrading.Common.Services.Settings;
using Rocks.Caching;

namespace MarginTrading.Backend.Services.Services
{
    /// <summary>
    /// Detects if margin trading of particular types (live and demo) is available globally and for user.
    /// </summary>
    public class MarginTradingEnabledCacheService : IMarginTradingSettingsCacheService
    {
        private static readonly CachingParameters ClientTradingEnabledCachingParameters =
            CachingParameters.InfiniteCache;

        private readonly IClientAccountService _clientAccountService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly ICacheProvider _cacheProvider;

        public MarginTradingEnabledCacheService(ICacheProvider cacheProvider,
            IClientAccountService clientAccountService, IAccountsCacheService accountsCacheService)
        {
            _cacheProvider = cacheProvider;
            _clientAccountService = clientAccountService;
            _accountsCacheService = accountsCacheService;
        }

        public Task<EnabledMarginTradingTypes> IsMarginTradingEnabled(string clientId)
            => IsMarginTradingEnabledInternal(clientId);

        public async Task<bool> IsMarginTradingEnabled(string clientId, bool isLive)
        {
            var enabledTypes = await IsMarginTradingEnabled(clientId);
            return isLive ? enabledTypes.Live : enabledTypes.Demo;
        }

        public bool IsMarginTradingEnabledByAccountId(string accountId)
        {
            return !(_accountsCacheService.TryGet(accountId)?.IsDisabled ?? true); // todo review and fix?
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
                var marginEnabledSettings = await _clientAccountService.GetMarginEnabledAsync(clientId);
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