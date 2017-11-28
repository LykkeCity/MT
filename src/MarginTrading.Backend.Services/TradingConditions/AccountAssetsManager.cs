using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public class AccountAssetsManager
    {
        private readonly AccountAssetsCacheService _accountAssetsCacheService;
        private readonly IAccountAssetPairsRepository _pairsRepository;
        private readonly MarginSettings _settings;
        private readonly IClientNotifyService _clientNotifyService;

        public AccountAssetsManager(
            AccountAssetsCacheService accountAssetsCacheService,
            IAccountAssetPairsRepository accountAssetPairsRepository,
            MarginSettings settings,
            IClientNotifyService clientNotifyService)
        {
            _accountAssetsCacheService = accountAssetsCacheService;
            _pairsRepository = accountAssetPairsRepository;
            _settings = settings;
            _clientNotifyService = clientNotifyService;
        }

        public void Start()
        {
            UpdateAccountAssetsCache().Wait();
        }

        public async Task UpdateAccountAssetsCache()
        {
            var accountAssets = (await _pairsRepository.GetAllAsync()).ToList();
            _accountAssetsCacheService.InitAccountAssetsCache(accountAssets);
        }

        public async Task<IEnumerable<IAccountAssetPair>> AssignInstruments(string tradingConditionId,
            string baseAssetId, string[] instruments)
        {
            var defaults = _settings.DefaultAccountAssetsSettings ?? new AccountAssetsSettings();
            var assignedPairs =
                await _pairsRepository.AssignAssetPairs(tradingConditionId, baseAssetId, instruments, defaults);
            await UpdateAccountAssetsCache();

            await _clientNotifyService.NotifyTradingConditionsChanged(tradingConditionId);

            return assignedPairs;
        }

        public async Task<IAccountAssetPair> AddOrReplaceAccountAssetAsync(IAccountAssetPair model)
        {
            await _pairsRepository.AddOrReplaceAsync(model);
            await UpdateAccountAssetsCache();
            
            await _clientNotifyService.NotifyTradingConditionsChanged(model.TradingConditionId);

            return model;
        }
    }
}
