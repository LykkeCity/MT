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
        private readonly MarginSettings _settings;
        private readonly IClientNotifyService _clientNotifyService;
        private readonly IOrderReader _orderReader;

        public AccountAssetsManager(
            AccountAssetsCacheService accountAssetsCacheService,
            IAccountAssetPairsRepository accountAssetPairsRepository,
            MarginSettings settings,
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

        public async Task<IEnumerable<IAccountAssetPair>> AssignInstruments(string tradingConditionId,
            string baseAssetId, string[] instruments)
        {
            var defaults = _settings.DefaultAccountAssetsSettings ?? new AccountAssetsSettings();
            
            var currentInstruments = (await _pairsRepository.GetAllAsync(tradingConditionId, baseAssetId)).ToArray();

            if (currentInstruments.Any())
            {
                var toRemove = currentInstruments.Where(x => !instruments.Contains(x.Instrument)).ToArray();

                var existingOrderGroups = (await _orderReader.GetAll())
                    .Where(o => o.TradingConditionId == tradingConditionId && o.AccountAssetId == baseAssetId)
                    .GroupBy(o => o.Instrument)
                    .Where(o => toRemove.Any(i => i.Instrument == o.Key))
                    .ToArray();

                if (existingOrderGroups.Any())
                {
                    var errorMessage = "Unable to remove following instruments as they have active orders: ";

                    foreach (var group in existingOrderGroups)
                    {
                        errorMessage += $"{group.Key}({group.Count()} orders) ";
                    }

                    throw new InvalidOperationException(errorMessage);
                }
                
                foreach (var pair in toRemove)
                {
                    await _pairsRepository.Remove(pair.TradingConditionId, pair.BaseAssetId, pair.Instrument);
                }
            }

            var pairsToAdd = instruments.Where(x => currentInstruments.All(y => y.Instrument != x));
            
            var addedPairs = await _pairsRepository.AddAssetPairs(tradingConditionId, baseAssetId, pairsToAdd, defaults);
            await UpdateAccountAssetsCache();

            await _clientNotifyService.NotifyTradingConditionsChanged(tradingConditionId);

            return addedPairs;
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
