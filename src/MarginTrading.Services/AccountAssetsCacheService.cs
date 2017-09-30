using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MarginTrading.Core;
using MarginTrading.Core.Messages;

namespace MarginTrading.Services
{
    public class AccountAssetsCacheService : IAccountAssetsCacheService
    {
        private Dictionary<(string, string), IAccountAssetPair[]> _accountGroupCache =
            new Dictionary<(string, string), IAccountAssetPair[]>();
        private Dictionary<(string, string, string), IAccountAssetPair> _instrumentsCache =
            new Dictionary<(string, string, string), IAccountAssetPair>();
        private HashSet<string> _instruments = new HashSet<string>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public IAccountAssetPair GetAccountAsset(string tradingConditionId, string accountAssetId, string instrument)
        {
            IAccountAssetPair accountAssetPair = null;

            _lockSlim.EnterReadLock();
            try
            {
                var key = GetInstrumentCacheKey(tradingConditionId, accountAssetId, instrument);

                if (_instrumentsCache.ContainsKey(key))
                    accountAssetPair = _instrumentsCache[key];
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }

            if (accountAssetPair == null)
            {
                throw new Exception(string.Format(MtMessages.AccountAssetForTradingConditionNotFound,
                    tradingConditionId, accountAssetId, instrument));
            }

            if (accountAssetPair.LeverageMaintenance < 1)
            {
                throw new Exception(string.Format(MtMessages.LeverageMaintanceIncorrect, tradingConditionId,
                    accountAssetId, instrument));
            }

            if (accountAssetPair.LeverageInit < 1)
            {
                throw new Exception(string.Format(MtMessages.LeverageInitIncorrect, tradingConditionId, accountAssetId,
                    instrument));
            }

            return accountAssetPair;
        }

        public IAccountAssetPair GetAccountAssetThrowIfNotFound(string tradingConditionId,
            string accountAssetId, string instrument)
        {
            IAccountAssetPair accountAssetPair = null;

            _lockSlim.EnterReadLock();
            try
            {
                var key = GetInstrumentCacheKey(tradingConditionId, accountAssetId, instrument);

                if (_instrumentsCache.ContainsKey(key))
                    accountAssetPair = _instrumentsCache[key];
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }

            if (accountAssetPair == null)
            {
                throw new Exception(string.Format(MtMessages.AccountAssetForTradingConditionNotFound,
                    tradingConditionId, accountAssetId, instrument));
            }

            return accountAssetPair;
        }

        public Dictionary<string, IAccountAssetPair[]> GetClientAssets(
            IEnumerable<MarginTradingAccount> accounts)
        {
            var result = new Dictionary<string, IAccountAssetPair[]>();

            if (accounts == null)
            {
                return result;
            }

            _lockSlim.EnterReadLock();
            try
            {
                foreach (var account in accounts)
                {
                    var key = GetAccountGroupCacheKey(account.TradingConditionId, account.BaseAssetId);

                    if (!result.ContainsKey(account.BaseAssetId) && _accountGroupCache.ContainsKey(key))
                    {
                        result.Add(account.BaseAssetId, _accountGroupCache[key]);
                    }
                }
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
            
            return result;
        }

        public ICollection<IAccountAssetPair> GetAccountAssets(string tradingConditionId, string baseAssetId)
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _accountGroupCache[GetAccountGroupCacheKey(tradingConditionId, baseAssetId)].ToList();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        internal void InitAccountAssetsCache(List<IAccountAssetPair> accountAssets)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _accountGroupCache = accountAssets
                    .GroupBy(a => GetAccountGroupCacheKey(a.TradingConditionId, a.BaseAssetId))
                    .ToDictionary(g => g.Key, g => g.ToArray());

                _instrumentsCache = accountAssets
                    .GroupBy(a => GetInstrumentCacheKey(a.TradingConditionId, a.BaseAssetId, a.Instrument))
                    .ToDictionary(g => g.Key, g => g.SingleOrDefault());

                _instruments = new HashSet<string>(accountAssets.Select(a => a.Instrument).Distinct());
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        private (string, string) GetAccountGroupCacheKey(string tradingCondition, string assetId)
        {
            return (tradingCondition, assetId);
        }

        private (string, string, string) GetInstrumentCacheKey(string tradingCondition, string assetId, string instrument)
        {
            return (tradingCondition, assetId, instrument);
        }
    }
}