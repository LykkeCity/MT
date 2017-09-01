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
        private List<IAccountAssetPair> _accountAssets = new List<IAccountAssetPair>();
        private HashSet<string> _instruments = new HashSet<string>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public IAccountAssetPair GetAccountAsset(string tradingConditionId, string accountAssetId,
            string instrument)
        {
            IAccountAssetPair accountAssetPair;

            _lockSlim.EnterReadLock();
            try
            {
                accountAssetPair = _accountAssets.FirstOrDefault(item =>
                    item.TradingConditionId == tradingConditionId && item.BaseAssetId == accountAssetId &&
                    item.Instrument == instrument);
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
            IAccountAssetPair accountAssetPair;

            _lockSlim.EnterReadLock();
            try
            {
                accountAssetPair = _accountAssets.FirstOrDefault(item =>
                    item.TradingConditionId == tradingConditionId && item.BaseAssetId == accountAssetId &&
                    item.Instrument == instrument);
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
                    if (!result.ContainsKey(account.BaseAssetId))
                    {
                        result.Add(account.BaseAssetId,
                            _accountAssets.Where(item =>
                                item.TradingConditionId == account.TradingConditionId &&
                                item.BaseAssetId == account.BaseAssetId).ToArray());
                    }
                }
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }

            return result;
        }

        public List<string> GetAccountAssetIds(string tradingConditionId, string baseAssetId)
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _accountAssets.Where(item =>
                        item.TradingConditionId == tradingConditionId && item.BaseAssetId == baseAssetId)
                    .Select(item => item.Instrument).ToList();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public List<IAccountAssetPair> GetAccountAssets(string tradingConditionId, string baseAssetId)
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _accountAssets.Where(item =>
                        item.TradingConditionId == tradingConditionId && item.BaseAssetId == baseAssetId)
                    .ToList();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public bool IsInstrumentSupported(string instrument)
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _instruments.Contains(instrument);
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
                _accountAssets = accountAssets;
                _instruments = new HashSet<string>(accountAssets.Select(a => a.Instrument).Distinct());
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }
    }
}