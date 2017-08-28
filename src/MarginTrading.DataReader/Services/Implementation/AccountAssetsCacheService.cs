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
        private readonly IMarginTradingAccountAssetRepository _repository;
        private readonly ICacheProvider _cacheProvider;

        public AccountAssetsCacheService(ICacheProvider cacheProvider, IMarginTradingAccountAssetRepository repository)
        {
            _cacheProvider = cacheProvider;
            _repository = repository;
        }

        private List<IMarginTradingAccountAsset> GetAccountAssetsCached()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            return _cacheProvider.Get(nameof(AccountAssetsCacheService),
                () =>
                {
                    var readTask = _repository.GetAllAsync();
                    var accountAssets = readTask.GetAwaiter().GetResult().ToList() ??
                                         new List<IMarginTradingAccountAsset>();
                    return new CachableResult<List<IMarginTradingAccountAsset>>(accountAssets, CachingParameters.FromSeconds(10));
                });
        }

        private List<IMarginTradingAccountAsset> AccountAssets => GetAccountAssetsCached();

        public IMarginTradingAccountAsset GetAccountAsset(string tradingConditionId, string accountAssetId,
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

        public IMarginTradingAccountAsset GetAccountAssetThrowIfNotFound(string tradingConditionId,
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

        public Dictionary<string, IMarginTradingAccountAsset[]> GetClientAssets(
            IEnumerable<MarginTradingAccount> accounts)
        {
            var result = new Dictionary<string, IMarginTradingAccountAsset[]>();

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

        public List<IMarginTradingAccountAsset> GetAccountAssets(string tradingConditionId, string baseAssetId)
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
