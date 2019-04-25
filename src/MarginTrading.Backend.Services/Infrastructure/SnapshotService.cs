using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
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
        
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public SnapshotService(
            IScheduleSettingsCacheService scheduleSettingsCacheService,
            IAccountsCacheService accountsCacheService,
            IQuoteCacheService quoteCacheService,
            IFxRateCacheService fxRateCacheService,
            IOrderReader orderReader,
            IDateService dateService,
            ITradingEngineSnapshotsRepository tradingEngineSnapshotsRepository)
        {
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
            _accountsCacheService = accountsCacheService;
            _quoteCacheService = quoteCacheService;
            _fxRateCacheService = fxRateCacheService;
            _orderReader = orderReader;
            _dateService = dateService;
            _tradingEngineSnapshotsRepository = tradingEngineSnapshotsRepository;
        }

        public async Task<string> MakeTradingDataSnapshot(DateTime tradingDay, string correlationId)
        {
            if (!_scheduleSettingsCacheService.TryGetPlatformCurrentDisabledInterval(out var disabledInterval))
            {
                throw new Exception(
                    "Trading should be stopped for whole platform in order to make trading data snapshot.");
            }

            if (disabledInterval.Start.AddDays(-1) > tradingDay.Date || disabledInterval.End < tradingDay.Date)
            {
                throw new Exception(
                    $"{nameof(tradingDay)}'s Date component must be from current disabled interval's Start -1d to End: [{disabledInterval.Start.AddDays(-1)}, {disabledInterval.End}].");
            }

            await _semaphoreSlim.WaitAsync();

            try
            {
                var orders = _orderReader.GetAllOrders();
                var positions = _orderReader.GetPositions();
                var accountStats = _accountsCacheService.GetAll();
                var bestFxPrices = _fxRateCacheService.GetAllQuotes();
                var bestPrices = _quoteCacheService.GetAllQuotes();

                await _tradingEngineSnapshotsRepository.Add(tradingDay, 
                    correlationId, 
                    _dateService.Now(), 
                    orders.Select(x => x.ConvertToContract(_orderReader)).ToJson(),
                    positions.Select(x => x.ConvertToContract(_orderReader)).ToJson(),
                    accountStats.Select(x => x.ConvertToContract()).ToJson(),
                    bestFxPrices.ToDictionary(q => q.Key, q => q.Value.ConvertToContract()).ToJson(),
                    bestPrices.ToDictionary(q => q.Key, q => q.Value.ConvertToContract()).ToJson());

                return $@"Trading data snapshot was written to the storage. 
Orders: {orders.Length}, positions: {positions.Length}, accounts: {accountStats.Count}, 
best FX prices: {bestFxPrices.Count}, best trading prices: {bestPrices.Count}.";
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}