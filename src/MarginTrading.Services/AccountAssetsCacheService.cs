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
        private readonly IInstrumentsCache _instrumentsCache;
        private List<IMarginTradingAccountAsset> _accountAssets = new List<IMarginTradingAccountAsset>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public AccountAssetsCacheService(IInstrumentsCache instrumentsCache)
        {
            _instrumentsCache = instrumentsCache;
        }

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

        public Dictionary<string, List<MarginTradingAsset>> GetClientAssets(IEnumerable<MarginTradingAccount> accounts)
        {
            var result = new Dictionary<string, List<MarginTradingAsset>>();

            if (accounts == null)
                return result;

            _lockSlim.EnterReadLock();
            try
            {
                foreach (var account in accounts)
                {
                    var accountAssets = _accountAssets.Where(item => item.TradingConditionId == account.TradingConditionId && item.BaseAssetId == account.BaseAssetId).ToDictionary(item => item.Instrument);

                    var assets =
                        _instrumentsCache.GetAll()
                        .Where(item => accountAssets.Keys.Contains(item.Id))
                        .Select(MarginTradingAsset.Create).ToList();

                    foreach (var asset in assets)
                    {
                        asset.LeverageInit = accountAssets[asset.Id].LeverageInit;
                        asset.LeverageMaintenance = accountAssets[asset.Id].LeverageMaintenance;
                        asset.DeltaBid = accountAssets[asset.Id].DeltaBid;
                        asset.DeltaAsk = accountAssets[asset.Id].DeltaAsk;
                        asset.SwapLong = accountAssets[asset.Id].SwapLong;
                        asset.SwapShort = accountAssets[asset.Id].SwapShort;
                    }

                    result.Add(account.Id, assets);
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

        internal void InitAccountAssetsCache(List<IMarginTradingAccountAsset> accountAssets)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _accountAssets = accountAssets;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }
    }
}
