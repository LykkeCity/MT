using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Core;
using MarginTrading.Core.Messages;
using Rocks.Caching;

namespace MarginTrading.DataReader.Services.Implementation
{
    public class AccountAssetsCacheService: IAccountAssetsCacheService
    {
        private readonly IAccountAssetPairsRepository _pairsRepository;
        private readonly ICacheProvider _cacheProvider;

        public AccountAssetsCacheService(ICacheProvider cacheProvider, IAccountAssetPairsRepository pairsRepository)
        {
            _cacheProvider = cacheProvider;
            _pairsRepository = pairsRepository;
        }

        private List<IAccountAssetPair> GetAccountAssetsCached()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            return _cacheProvider.Get(nameof(AccountAssetsCacheService),
                () =>
                {
                    var readTask = _pairsRepository.GetAllAsync();
                    var accountAssets = readTask.GetAwaiter().GetResult().ToList() ??
                                         new List<IAccountAssetPair>();
                    return new CachableResult<List<IAccountAssetPair>>(accountAssets, CachingParameters.FromSeconds(10));
                });
        }

        private List<IAccountAssetPair> AccountAssets => GetAccountAssetsCached();

        public IAccountAssetPair GetAccountAsset(string tradingConditionId, string accountAssetId,
            string instrument)
        {
            var accountAsset = AccountAssets.FirstOrDefault(item =>
                item.TradingConditionId == tradingConditionId && item.BaseAssetId == accountAssetId &&
                item.Instrument == instrument);

            if (accountAsset == null)
            {
                throw new Exception(string.Format(MtMessages.AccountAssetForTradingConditionNotFound,
                    tradingConditionId, accountAssetId, instrument));
            }

            if (accountAsset.LeverageMaintenance < 1)
            {
                throw new Exception(string.Format(MtMessages.LeverageMaintanceIncorrect, tradingConditionId,
                    accountAssetId, instrument));
            }

            if (accountAsset.LeverageInit < 1)
            {
                throw new Exception(string.Format(MtMessages.LeverageInitIncorrect, tradingConditionId, accountAssetId,
                    instrument));
            }

            return accountAsset;
        }

        public IAccountAssetPair GetAccountAssetThrowIfNotFound(string tradingConditionId,
            string accountAssetId, string instrument)
        {
            var accountAsset = AccountAssets.FirstOrDefault(item =>
                item.TradingConditionId == tradingConditionId && item.BaseAssetId == accountAssetId &&
                item.Instrument == instrument);

            if (accountAsset == null)
            {
                throw new Exception(string.Format(MtMessages.AccountAssetForTradingConditionNotFound,
                    tradingConditionId, accountAssetId, instrument));
            }

            return accountAsset;
        }

        public Dictionary<string, IAccountAssetPair[]> GetClientAssets(
            IEnumerable<MarginTradingAccount> accounts)
        {
            var result = new Dictionary<string, IAccountAssetPair[]>();

            if (accounts == null)
            {
                return result;
            }

            foreach (var account in accounts)
            {
                if (!result.ContainsKey(account.BaseAssetId))
                {
                    result.Add(account.BaseAssetId,
                        AccountAssets.Where(item =>
                            item.TradingConditionId == account.TradingConditionId &&
                            item.BaseAssetId == account.BaseAssetId).ToArray());
                }
            }

            return result;
        }

        public List<string> GetAccountAssetIds(string tradingConditionId, string baseAssetId)
        {
            return AccountAssets.Where(item =>
                    item.TradingConditionId == tradingConditionId && item.BaseAssetId == baseAssetId)
                .Select(item => item.Instrument).ToList();
        }

        public List<IAccountAssetPair> GetAccountAssets(string tradingConditionId, string baseAssetId)
        {
            return AccountAssets.Where(item =>
                    item.TradingConditionId == tradingConditionId && item.BaseAssetId == baseAssetId)
                .ToList();
        }

        public bool IsInstrumentSupported(string instrument)
        {
            return AccountAssets.Any(a => a.Instrument == instrument);
        }
    }
}
