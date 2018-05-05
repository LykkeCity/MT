using System;
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
        private readonly MarginTradingSettings _settings;
        private readonly IClientNotifyService _clientNotifyService;
        private readonly IOrderReader _orderReader;

        public AccountAssetsManager(
            AccountAssetsCacheService accountAssetsCacheService,
            IAccountAssetPairsRepository accountAssetPairsRepository,
            MarginTradingSettings settings,
            IClientNotifyService clientNotifyService,
            IOrderReader orderReader)
        {
            _accountAssetsCacheService = accountAssetsCacheService;
            _pairsRepository = accountAssetPairsRepository;
            _settings = settings;
            _clientNotifyService = clientNotifyService;
            _orderReader = orderReader;
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
    }
}
