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
        private List<IMarginTradingAccountAsset> _accountAssets = new List<IMarginTradingAccountAsset>();
        private HashSet<string> _instruments = new HashSet<string>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        ~AccountAssetsCacheService()
        {
            _lockSlim?.Dispose();
        }

        public IMarginTradingAccountAsset GetAccountAsset(string tradingConditionId, string accountAssetId, string instrument)
        {
            IMarginTradingAccountAsset accountAsset;

            _lockSlim.EnterReadLock();
            try
            {
                accountAsset = _accountAssets.FirstOrDefault(item => item.TradingConditionId == tradingConditionId && item.BaseAssetId == accountAssetId && item.Instrument == instrument);
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }

            if (accountAsset == null)
            {
                throw new Exception(string.Format(MtMessages.AccountAssetForTradingConditionNotFound, tradingConditionId, accountAssetId, instrument));
            }

            if (accountAsset.LeverageMaintenance < 1)
            {
                throw new Exception(string.Format(MtMessages.LeverageMaintanceIncorrect, tradingConditionId, accountAssetId, instrument));
            }

            if (accountAsset.LeverageInit < 1)
            {
                throw new Exception(string.Format(MtMessages.LeverageInitIncorrect, tradingConditionId, accountAssetId, instrument));
            }

            return accountAsset;
        }

        public IMarginTradingAccountAsset GetAccountAssetNoThrowExceptionOnInvalidData(string tradingConditionId,
            string accountAssetId, string instrument)
        {
            IMarginTradingAccountAsset accountAsset;

            _lockSlim.EnterReadLock();
            try
            {
                accountAsset = _accountAssets.FirstOrDefault(item => item.TradingConditionId == tradingConditionId && item.BaseAssetId == accountAssetId && item.Instrument == instrument);
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }

            if (accountAsset == null)
            {
                throw new Exception(string.Format(MtMessages.AccountAssetForTradingConditionNotFound, tradingConditionId, accountAssetId, instrument));
            }

            return accountAsset;
        }

        public Dictionary<string, IMarginTradingAccountAsset[]> GetClientAssets(IEnumerable<MarginTradingAccount> accounts)
        {
            var result = new Dictionary<string, IMarginTradingAccountAsset[]>();

            if (accounts == null)
                return result;

            _lockSlim.EnterReadLock();
            try
            {
                foreach (var account in accounts)
                {
                    if (!result.ContainsKey(account.BaseAssetId))
                    {
                        result.Add(account.BaseAssetId, _accountAssets.Where(item => item.TradingConditionId == account.TradingConditionId && item.BaseAssetId == account.BaseAssetId).ToArray());
                    }
                }
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
            
            return result;
        }

        public List<string> GetAccountAssetIds(string tradingConditionId, string accountAssetId)
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _accountAssets.Where(item => item.TradingConditionId == tradingConditionId && item.BaseAssetId == accountAssetId)
                    .Select(item => item.Instrument).ToList();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public List<IMarginTradingAccountAsset> GetAccountAssets(string tradingConditionId, string accountAssetId)
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _accountAssets.Where(item => item.TradingConditionId == tradingConditionId && item.BaseAssetId == accountAssetId)
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

        internal void InitAccountAssetsCache(List<IMarginTradingAccountAsset> accountAssets)
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
