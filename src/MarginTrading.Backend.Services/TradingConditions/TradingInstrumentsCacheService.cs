using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public class TradingInstrumentsCacheService : ITradingInstrumentsCacheService, IOvernightMarginParameterContainer
    {
        private readonly OvernightMarginSettings _overnightMarginSettings;
        
        private Dictionary<(string, string), ITradingInstrument> _instrumentsCache =
            new Dictionary<(string, string), ITradingInstrument>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public decimal OvernightMarginParameter { get; set; } = 1;

        public TradingInstrumentsCacheService(
            OvernightMarginSettings overnightMarginSettings)
        {
            _overnightMarginSettings = overnightMarginSettings;
        }

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

        public (decimal MarginInit, decimal MarginMaintenance) GetMarginRates(ITradingInstrument tradingInstrument,
            bool isWarnCheck = false)
        {
            var parameter = isWarnCheck ? _overnightMarginSettings.OvernightMarginParameter : OvernightMarginParameter;
            
            return (1 / (tradingInstrument.LeverageInit * parameter), 
                1 / (tradingInstrument.LeverageMaintenance * parameter));
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