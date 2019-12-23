// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Mappers;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;

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
        private readonly ILog _log;

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public SnapshotService(
            IScheduleSettingsCacheService scheduleSettingsCacheService,
            IAccountsCacheService accountsCacheService,
            IQuoteCacheService quoteCacheService,
            IFxRateCacheService fxRateCacheService,
            IOrderReader orderReader,
            IDateService dateService,
            ITradingEngineSnapshotsRepository tradingEngineSnapshotsRepository,
            ILog log)
        {
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
            _accountsCacheService = accountsCacheService;
            _quoteCacheService = quoteCacheService;
            _fxRateCacheService = fxRateCacheService;
            _orderReader = orderReader;
            _dateService = dateService;
            _tradingEngineSnapshotsRepository = tradingEngineSnapshotsRepository;
            _log = log;
        }

        public async Task<string> MakeTradingDataSnapshot(DateTime tradingDay, string correlationId)
        {
            if (!_scheduleSettingsCacheService.TryGetPlatformCurrentDisabledInterval(out var disabledInterval))
            {
                //TODO: remove later (if everything will work and we will never go to this branch)
                _scheduleSettingsCacheService.MarketsCacheWarmUp();
                
                if (!_scheduleSettingsCacheService.TryGetPlatformCurrentDisabledInterval(out disabledInterval))
                {
                    throw new Exception(
                        $"Trading should be stopped for whole platform in order to make trading data snapshot. Current schedule: {_scheduleSettingsCacheService.GetPlatformTradingSchedule()?.ToJson()}");
                }
            }

            if (disabledInterval.Start.AddDays(-1) > tradingDay.Date || disabledInterval.End < tradingDay.Date)
            {
                throw new Exception(
                    $"{nameof(tradingDay)}'s Date component must be from current disabled interval's Start -1d to End: [{disabledInterval.Start.AddDays(-1)}, {disabledInterval.End}].");
            }

            if (_semaphoreSlim.CurrentCount == 0)
            {
                throw new ArgumentException("Trading data snapshot creation is already in progress", "snapshot");
            }
            
            await _semaphoreSlim.WaitAsync();

            try
            {
                var orders = _orderReader.GetAllOrders();
                var ordersData = orders.Select(x => x.ConvertToContract(_orderReader)).ToJson();
                await _log.WriteInfoAsync(nameof(SnapshotService), nameof(MakeTradingDataSnapshot),
                    $"Preparing data... {orders.Length} orders prepared.");
                
                var positions = _orderReader.GetPositions();
                var positionsData = positions.Select(x => x.ConvertToContract(_orderReader)).ToJson();
                await _log.WriteInfoAsync(nameof(SnapshotService), nameof(MakeTradingDataSnapshot),
                    $"Preparing data... {positions.Length} positions prepared.");
                
                var accountStats = _accountsCacheService.GetAll();
                var accountStatsData = accountStats.Select(x => x.ConvertToContract()).ToJson();
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

                await _tradingEngineSnapshotsRepository.Add(tradingDay, correlationId, _dateService.Now(), 
                    ordersData, positionsData, accountStatsData, bestFxPricesData, 
                    bestPricesData);

                await _log.WriteInfoAsync(nameof(SnapshotService), nameof(MakeTradingDataSnapshot),
                    $"Trading data snapshot was written to the storage. {msg}");   
                return $"Trading data snapshot was written to the storage. {msg}";
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}