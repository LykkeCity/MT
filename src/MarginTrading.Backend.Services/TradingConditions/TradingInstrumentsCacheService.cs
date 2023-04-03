// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
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
        private readonly IOrderReader _orderReader;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly MarginTradingSettings _marginTradingSettings;

        private Dictionary<(string, string), ITradingInstrument> _instrumentsCache =
            new Dictionary<(string, string), ITradingInstrument>();

        private bool _overnightMarginParameterOn;

        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public TradingInstrumentsCacheService(
            ICqrsSender cqrsSender,
            IIdentityGenerator identityGenerator,
            IDateService dateService,
            ILog log,
            IOrderReader orderReader,
            IAccountsCacheService accountsCacheService,
            MarginTradingSettings marginTradingSettings)
        {
            _cqrsSender = cqrsSender;
            _identityGenerator = identityGenerator;
            _dateService = dateService;
            _log = log;
            _orderReader = orderReader;
            _accountsCacheService = accountsCacheService;
            _marginTradingSettings = marginTradingSettings;
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

            if (accountAssetPair.MaintenanceLeverage < 1)
            {
                throw new Exception(
                    string.Format(MtMessages.LeverageMaintanceIncorrect, tradingConditionId, instrument));
            }

            if (accountAssetPair.InitLeverage < 1)
            {
                throw new Exception(string.Format(MtMessages.LeverageInitIncorrect, tradingConditionId,
                    instrument));
            }

            return accountAssetPair;
        }

        public (decimal MarginInit, decimal MarginMaintenance) GetMarginRates(ITradingInstrument tradingInstrument,
            bool isWarnCheck = false)
        {
            decimal marginInit = tradingInstrument.GetMarginInitByLeverage(_overnightMarginParameterOn, isWarnCheck);
            decimal marginMaintenance = tradingInstrument.GetMarginMaintenanceByLeverage(_overnightMarginParameterOn, isWarnCheck);

            if(_marginTradingSettings.LogBlockedMarginCalculation && SnapshotService.IsMakingSnapshotInProgress)
            {
                _log.WriteInfo(nameof(TradingInstrumentsCacheService), tradingInstrument.ToJson(), 
                    "Getting marginInit and marginMaintenance values by leverage");
            }

            return (marginInit, marginMaintenance);
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
                foreach (var position in _orderReader.GetPositions())
                {
                    position.FplDataShouldBeRecalculated();
                }

                foreach (var account in _accountsCacheService.GetAll().Where(a => a.GetOpenPositionsCount() > 0))
                {
                    account.CacheNeedsToBeUpdated();
                }
                
                //send event when the overnight margin parameter is enabled/disabled (margin requirement changes)
                _cqrsSender.PublishEvent(new OvernightMarginParameterChangedEvent
                {
                    CorrelationId = _identityGenerator.GenerateGuid(),
                    EventTimestamp = _dateService.Now(),
                    CurrentState = _overnightMarginParameterOn,
                    ParameterValues = GetOvernightMarginParameterValues(true),
                });
            }
        }

        public Dictionary<(string, string), decimal> GetOvernightMarginParameterValues(bool onlyNotEqualToOne = false)
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _instrumentsCache
                    .Where(x => !onlyNotEqualToOne || x.Value.OvernightMarginMultiplier != 1)
                    .ToDictionary(x => x.Key, x => _overnightMarginParameterOn
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