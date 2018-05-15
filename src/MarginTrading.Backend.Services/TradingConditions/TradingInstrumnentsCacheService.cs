using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    [UsedImplicitly]
    public class  TradingInstrumnentsCacheService : ITradingInstrumnentsCacheService
    {
        private Dictionary<(string, string), ITradingInstrument> _instrumentsCache =
            new Dictionary<(string, string), ITradingInstrument>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public ITradingInstrument GetTradingInstrument(string tradingConditionId, string instrument)
        {
            ITradingInstrument accountAssetPair = null;

            _lockSlim.EnterReadLock();
            try
            {
                var key = GetInstrumentCacheKey(tradingConditionId, instrument);

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
                    tradingConditionId, instrument));
            }

            if (accountAssetPair.LeverageMaintenance < 1)
            {
                throw new Exception(
                    string.Format(MtMessages.LeverageMaintanceIncorrect, tradingConditionId, instrument));
            }

            if (accountAssetPair.LeverageInit < 1)
            {
                throw new Exception(string.Format(MtMessages.LeverageInitIncorrect, tradingConditionId,
                    instrument));
            }

            return accountAssetPair;
        }

        internal void InitAccountAssetsCache(List<ITradingInstrument> accountAssets)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _instrumentsCache = accountAssets
                    .GroupBy(a => GetInstrumentCacheKey(a.TradingConditionId, a.Instrument))
                    .ToDictionary(g => g.Key, g => g.SingleOrDefault());
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        private (string, string) GetInstrumentCacheKey(string tradingCondition, string instrument)
        {
            return (tradingCondition, instrument);
        }
    }
}