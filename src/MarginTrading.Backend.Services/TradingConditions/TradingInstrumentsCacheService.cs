using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Common.Log;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.TradingConditions
{
    /// <inheritdoc cref="ITradingInstrumentsCacheService" />
    /// <summary>
    /// Contains TradingInstruments cache and margin parameter state.
    /// </summary>
    public class TradingInstrumentsCacheService : ITradingInstrumentsCacheService, IOvernightMarginParameterContainer
    {
        private readonly ICqrsSender _cqrsSender;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly IDateService _dateService;
        private readonly ILog _log;
        
        private Dictionary<(string, string), ITradingInstrument> _instrumentsCache =
            new Dictionary<(string, string), ITradingInstrument>();

        private bool _overnightMarginParameterOn;

        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public TradingInstrumentsCacheService(
            ICqrsSender cqrsSender,
            IIdentityGenerator identityGenerator,
            IDateService dateService,
            ILog log)
        {
            _cqrsSender = cqrsSender;
            _identityGenerator = identityGenerator;
            _dateService = dateService;
            _log = log;
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
            var parameter = isWarnCheck || _overnightMarginParameterOn
                ? tradingInstrument.OvernightMarginMultiplier
                : 1;
            
            return (parameter / tradingInstrument.LeverageInit, parameter / tradingInstrument.LeverageMaintenance);
        }

        public void InitCache(IEnumerable<ITradingInstrument> tradingInstruments)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _instrumentsCache = tradingInstruments
                    .GroupBy(a => GetInstrumentCacheKey(a.TradingConditionId, a.Instrument))
                    .ToDictionary(g => g.Key, g => g.SingleOrDefault());
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public void UpdateCache(ITradingInstrument tradingInstrument)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                var key = GetInstrumentCacheKey(tradingInstrument.TradingConditionId, tradingInstrument.Instrument);

                _instrumentsCache[key] = tradingInstrument;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public void RemoveFromCache(string tradingConditionId, string instrument)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                var key = GetInstrumentCacheKey(tradingConditionId, instrument);

                _instrumentsCache.Remove(key);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public bool GetOvernightMarginParameterState() => _overnightMarginParameterOn;

        public void SetOvernightMarginParameterState(bool isOn)
        {
            var multiplierChanged = _overnightMarginParameterOn != isOn;

            _overnightMarginParameterOn = isOn;

            if (multiplierChanged)
            {
                //send event when the overnight margin parameter is enabled/disabled (margin requirement changes)
                _cqrsSender.PublishEvent(new OvernightMarginParameterChangedEvent
                {
                    CorrelationId = _identityGenerator.GenerateGuid(),
                    EventTimestamp = _dateService.Now(),
                    CurrentState = _overnightMarginParameterOn,
                    ParameterValues = GetOvernightMarginParameterValues()
                        .Where(x => x.Value != 1)
                        .ToDictionary(x => x.Key, x => x.Value),
                });
            }
        }

        public Dictionary<(string, string), decimal> GetOvernightMarginParameterValues()
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _instrumentsCache.ToDictionary(x => x.Key, x => _overnightMarginParameterOn
                    ? x.Value.OvernightMarginMultiplier
                    : 1);
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        private static (string, string) GetInstrumentCacheKey(string tradingCondition, string instrument) =>
            (tradingCondition, instrument);
    }
}