// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Mappers;
using MarginTrading.Common.Services;
using MoreLinq;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class SnapshotService : ISnapshotService
    {
        private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly IFxRateCacheService _fxRateCacheService;
        private readonly IOrderReader _orderReader;
        private readonly IDateService _dateService;

        private readonly ITradingEngineSnapshotsRepository _tradingEngineSnapshotsRepository;
        private readonly ISnapshotValidationService _snapshotValidationService;
        private readonly IQueueValidationService _queueValidationService;
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly ILog _log;
        private readonly IFinalSnapshotCalculator _finalSnapshotCalculator;
        private readonly MarginTradingSettings _settings;

        private static readonly SemaphoreSlim Lock = new SemaphoreSlim(1, 1);
        public static bool IsMakingSnapshotInProgress => Lock.CurrentCount == 0;

        public SnapshotService(
            IScheduleSettingsCacheService scheduleSettingsCacheService,
            IAccountsCacheService accountsCacheService,
            IQuoteCacheService quoteCacheService,
            IFxRateCacheService fxRateCacheService,
            IOrderReader orderReader,
            IDateService dateService,
            ITradingEngineSnapshotsRepository tradingEngineSnapshotsRepository,
            ISnapshotValidationService snapshotValidationService,
            IQueueValidationService queueValidationService,
            IMarginTradingBlobRepository blobRepository,
            ILog log,
            IFinalSnapshotCalculator finalSnapshotCalculator,
            MarginTradingSettings settings)
        {
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
            _accountsCacheService = accountsCacheService;
            _quoteCacheService = quoteCacheService;
            _fxRateCacheService = fxRateCacheService;
            _orderReader = orderReader;
            _dateService = dateService;
            _tradingEngineSnapshotsRepository = tradingEngineSnapshotsRepository;
            _snapshotValidationService = snapshotValidationService;
            _queueValidationService = queueValidationService;
            _blobRepository = blobRepository;
            _log = log;
            _finalSnapshotCalculator = finalSnapshotCalculator;
            _settings = settings;
        }

        /// <inheritdoc />
        public async Task<string> MakeTradingDataSnapshot(DateTime tradingDay, string correlationId, SnapshotStatus status = SnapshotStatus.Final)
        {
            if (!_scheduleSettingsCacheService.TryGetPlatformCurrentDisabledInterval(out var disabledInterval))
            {
                //TODO: remove later (if everything will work and we will never go to this branch)
                _scheduleSettingsCacheService.MarketsCacheWarmUp();
                
                if (!_scheduleSettingsCacheService.TryGetPlatformCurrentDisabledInterval(out disabledInterval))
                {
                    throw new Exception($"Trading should be stopped for whole platform in order to make trading data snapshot. Current schedule: {_scheduleSettingsCacheService.GetPlatformTradingSchedule()?.ToJson()}");
                }
            }

            if (disabledInterval.Start.AddDays(-1) > tradingDay.Date || disabledInterval.End < tradingDay.Date)
            {
                throw new Exception(
                    $"{nameof(tradingDay)}'s Date component must be from current disabled interval's Start -1d to End: [{disabledInterval.Start.AddDays(-1)}, {disabledInterval.End}].");
            }

            if (IsMakingSnapshotInProgress)
            {
                throw new InvalidOperationException("Trading data snapshot creation is already in progress");
            }

            // We must be sure all messages have been processed by history brokers before starting current state validation.
            // If one or more queues contain not delivered messages the snapshot can not be created.  
            _queueValidationService.ThrowExceptionIfQueuesNotEmpty(true);

            // Before starting snapshot creation the current state should be validated.
            var validationResult = await _snapshotValidationService.ValidateCurrentStateAsync();

            if (!validationResult.IsValid)
            {
                var ex = new InvalidOperationException(
                    $"The trading data snapshot might be corrupted. The current state of orders and positions is incorrect. Check the dbo.BlobData table for more info: container {LykkeConstants.MtCoreSnapshotBlobContainer}, correlationId {correlationId}");
                await _log.WriteFatalErrorAsync(nameof(SnapshotService), 
                    nameof(MakeTradingDataSnapshot),
                    validationResult.ToJson(),
                    ex);
                await _blobRepository.WriteAsync(LykkeConstants.MtCoreSnapshotBlobContainer, correlationId, validationResult);
            }
            else
            {
                await _log.WriteInfoAsync(nameof(SnapshotService), nameof(MakeTradingDataSnapshot),
                    "The current state of orders and positions is correct.");
            }

            await Lock.WaitAsync();

            try
            {
                var orders = _orderReader.GetAllOrders();
                var ordersJson = orders.Select(o => o.ConvertToSnapshotContract(_orderReader, status)).ToJson();
                await _log.WriteInfoAsync(nameof(SnapshotService), nameof(MakeTradingDataSnapshot),
                    $"Preparing data... {orders.Length} orders prepared.");
                
                var positions = _orderReader.GetPositions();
                var positionsJson = positions.Select(p => p.ConvertToSnapshotContract(_orderReader, status)).ToJson();
                await _log.WriteInfoAsync(nameof(SnapshotService), nameof(MakeTradingDataSnapshot),
                    $"Preparing data... {positions.Length} positions prepared.");
                
                var accountStats = _accountsCacheService.GetAll();

                if (_settings.LogBlockedMarginCalculation)
                {
                    foreach (var accountStat in accountStats)
                    {
                        var margin = accountStat.GetUsedMargin();
                        if (margin == 0) continue;
                        await _log.WriteInfoAsync(nameof(SnapshotService), nameof(MakeTradingDataSnapshot),
                            @$"Account {accountStat.Id}, TotalBlockedMargin {margin}, {accountStat.LogInfo}");
                    }
                }

                // Forcing all account caches to be updated after trading is closed - after all events have been processed
                // To ensure all cache data is updated with most up-to date data for all accounts.
                accountStats.ForEach(a => a.CacheNeedsToBeUpdated());

                var accountsInLiquidation = await _accountsCacheService.GetAllInLiquidation().ToListAsync();
                var accountsJson = accountStats
                    .Select(a => a.ConvertToSnapshotContract(accountsInLiquidation.Contains(a), status))
                    .ToJson();
                await _log.WriteInfoAsync(nameof(SnapshotService), nameof(MakeTradingDataSnapshot),
                    $"Preparing data... {accountStats.Count} accounts prepared.");
                
                var bestFxPrices = _fxRateCacheService.GetAllQuotes();
                var bestFxPricesData = bestFxPrices.ToDictionary(q => q.Key, q => q.Value.ConvertToContract()).ToJson();
                await _log.WriteInfoAsync(nameof(SnapshotService), nameof(MakeTradingDataSnapshot),
                    $"Preparing data... {bestFxPrices.Count} best FX prices prepared.");
                
                var bestPrices = _quoteCacheService.GetAllQuotes();
                var bestPricesData = bestPrices.ToDictionary(q => q.Key, q => q.Value.ConvertToContract()).ToJson();
                await _log.WriteInfoAsync(nameof(SnapshotService), nameof(MakeTradingDataSnapshot),
                    $"Preparing data... {bestPrices.Count} best trading prices prepared.");
                
                var msg = $"TradingDay: {tradingDay:yyyy-MM-dd}, Orders: {orders.Length}, positions: {positions.Length}, accounts: {accountStats.Count}, best FX prices: {bestFxPrices.Count}, best trading prices: {bestPrices.Count}.";

                await _log.WriteInfoAsync(nameof(SnapshotService), nameof(MakeTradingDataSnapshot),
                    $"Starting to write trading data snapshot. {msg}");

                var snapshot = new TradingEngineSnapshot(
                    tradingDay,
                    correlationId,
                    _dateService.Now(),
                    ordersJson: ordersJson,
                    positionsJson: positionsJson,
                    accountsJson: accountsJson,
                    bestFxPricesJson: bestFxPricesData,
                    bestTradingPricesJson: bestPricesData,
                    status: status);

                await _tradingEngineSnapshotsRepository.AddAsync(snapshot);

                await _log.WriteInfoAsync(nameof(SnapshotService), nameof(MakeTradingDataSnapshot),
                    $"Trading data snapshot was written to the storage. {msg}");   
                return $"Trading data snapshot was written to the storage. {msg}";
            }
            finally
            {
                Lock.Release();
            }
        }

        /// <inheritdoc />
        public async Task MakeTradingDataSnapshotFromDraft(
            string correlationId,
            IEnumerable<ClosingAssetPrice> cfdQuotes,
            IEnumerable<ClosingFxRate> fxRates)
        {
            if (IsMakingSnapshotInProgress)
            {
                throw new InvalidOperationException("Trading data snapshot manipulations are already in progress");
            }
            
            await Lock.WaitAsync();
            try
            {
                var snapshot = await _finalSnapshotCalculator.RunAsync(fxRates, cfdQuotes, correlationId);
                    
                await _tradingEngineSnapshotsRepository.AddAsync(snapshot);
            }
            finally
            {
                Lock.Release();
            }
        }
    }
}