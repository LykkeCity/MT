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
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Services.Extensions;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class FakeSnapshotService : IFakeSnapshotService
    {
        private readonly ITradingEngineSnapshotsRepository _tradingEngineSnapshotsRepository;
        private readonly IDateService _dateService;
        private readonly ILog _log;

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public FakeSnapshotService(ITradingEngineSnapshotsRepository tradingEngineSnapshotsRepository,
            IDateService dateService,
            ILog log)
        {
            _tradingEngineSnapshotsRepository = tradingEngineSnapshotsRepository;
            _dateService = dateService;
            _log = log;
        }

        public async Task<string> AddOrUpdateFakeTradingDataSnapshot(DateTime tradingDay,
            string correlationId,
            List<OrderContract> orders,
            List<OpenPositionContract> positions,
            List<AccountStatContract> accounts,
            Dictionary<string, BestPriceContract> bestFxPrices,
            Dictionary<string, BestPriceContract> bestTradingPrices)
        {
            if (_semaphoreSlim.CurrentCount == 0)
            {
                var handle = _semaphoreSlim.AvailableWaitHandle;
                var handleResult = handle.WaitOne(TimeSpan.FromMinutes(1));
                if (!handleResult) return "Operation timeout";
            }

            await _semaphoreSlim.WaitAsync();

            try
            {
                var msg =
                    $"TradingDay: {tradingDay:yyyy-MM-dd}, Orders: {orders.Count}, positions: {positions.Count}, accounts: {accounts.Count}, best FX prices: {bestFxPrices.Count}, best trading prices: {bestTradingPrices.Count}.";

                await _log.WriteInfoAsync(nameof(FakeSnapshotService), nameof(AddOrUpdateFakeTradingDataSnapshot),
                    $"Starting to write trading data snapshot. {msg}");

                var snapshot = await _tradingEngineSnapshotsRepository.Get(correlationId);
                if (snapshot != null)
                {
                    orders = Union(orders, snapshot.GetOrders(), order => order.Id);
                    positions = Union(positions, snapshot.GetPositions(), position => position.Id);
                    accounts = Union(accounts, snapshot.GetAccounts(), account => account.AccountId);
                    bestFxPrices = Union(bestFxPrices, snapshot.GetBestFxPrices());
                    bestTradingPrices = Union(bestTradingPrices, snapshot.GetBestTradingPrices());
                }

                var newSnapshot = new TradingEngineSnapshot(
                    tradingDay,
                    correlationId,
                    _dateService.Now(),
                    positionsJson: positions.ToJson(),
                    ordersJson: orders.ToJson(),
                    accountsJson: accounts.ToJson(),
                    bestFxPricesJson: bestFxPrices.ToJson(),
                    bestTradingPricesJson: bestTradingPrices.ToJson(),
                    status: SnapshotStatus.Final);

                await _tradingEngineSnapshotsRepository.AddAsync(newSnapshot);

                await _log.WriteInfoAsync(nameof(FakeSnapshotService), nameof(AddOrUpdateFakeTradingDataSnapshot),
                    $"Trading data snapshot was written to the storage. {msg}");
                return $"Trading data snapshot was written to the storage. {msg}";
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public Task DeleteFakeTradingSnapshot(string correlationId)
        {
            return _tradingEngineSnapshotsRepository.Delete(correlationId);
        }

        private List<T> Union<T>(List<T> values, List<T> oldValues, Func<T, string> id)
        {
            foreach (var value in values)
            {
                if (oldValues.Any(x => id(value) == id(x)))
                {
                    oldValues.RemoveAll(x => id(value) == id(x));
                }

                oldValues.Add(value);
            }

            return oldValues;
        }

        private Dictionary<string, T> Union<T>(Dictionary<string, T> values, Dictionary<string, T> oldValues)
        {
            foreach (var kvp in values)
            {
                if (oldValues.ContainsKey(kvp.Key))
                {
                    oldValues[kvp.Key] = kvp.Value;    
                }
                else
                {
                    oldValues.Add(kvp.Key, kvp.Value);
                }
            }

            return oldValues;
        }
    }
}