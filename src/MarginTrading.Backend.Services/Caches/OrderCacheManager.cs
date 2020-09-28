// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Helpers;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Helpers;

namespace MarginTrading.Backend.Services.Caches
{
    public class OrderCacheManager : TimerPeriod
    {
        private readonly OrdersCache _orderCache;
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly IOrdersHistoryRepository _ordersHistoryRepository;
        private readonly IPositionsHistoryRepository _positionsHistoryRepository;
        private readonly ILog _log;
        
        public const string OrdersBlobName= "orders";
        public const string PositionsBlobName= "positions";
        
        private static readonly OrderStatus[] OrderTerminalStatuses = {OrderStatus.Canceled, OrderStatus.Rejected, OrderStatus.Executed};
        private static readonly PositionHistoryType PositionTerminalStatus = PositionHistoryType.Close;

        public OrderCacheManager(OrdersCache orderCache,
            IMarginTradingBlobRepository blobRepository,
            IOrdersHistoryRepository ordersHistoryRepository,
            IPositionsHistoryRepository positionsHistoryRepository,
            MarginTradingSettings marginTradingSettings,
            ILog log) 
            : base(nameof(OrderCacheManager), marginTradingSettings.BlobPersistence.OrdersDumpPeriodMilliseconds, log)
        {
            _orderCache = orderCache;
            _blobRepository = blobRepository;
            _ordersHistoryRepository = ordersHistoryRepository;
            _positionsHistoryRepository = positionsHistoryRepository;
            _log = log;
        }

        public override void Start()
        {
            InferInitDataFromBlobAndHistory();

            base.Start();
        }

        public override async Task Execute()
        {
            await Task.WhenAll(DumpOrdersToRepository(), DumpPositionsToRepository());
        }

        public override void Stop()
        {
            DumpOrdersToRepository().Wait();
            DumpPositionsToRepository().Wait();
            base.Stop();
        }

        private async Task DumpOrdersToRepository()
        {
            try
            {
                var orders = _orderCache.GetAllOrders();

                if (orders != null)
                {
                    await _blobRepository.WriteAsync(LykkeConstants.StateBlobContainer, OrdersBlobName, orders);
                }
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(OrdersCache), "Save orders", "", ex);
            }
        }

        private async Task DumpPositionsToRepository()
        {
            try
            {
                var positions = _orderCache.GetPositions();

                if (positions != null)
                {
                    await _blobRepository.WriteAsync(LykkeConstants.StateBlobContainer, PositionsBlobName, positions);
                }
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(OrdersCache), "Save positions", "", ex);
            }
        }

        /// <summary>
        /// Infer init data from blob and history.
        /// </summary>
        private (List<Order> Orders, List<Position> Positions) InferInitDataFromBlobAndHistory()
        {
            _log.WriteInfo(nameof(OrderCacheManager), nameof(InferInitDataFromBlobAndHistory),
                $"Start reading order and position data from blob.");

            var blobOrdersTask = _blobRepository.ReadWithTimestampAsync<List<Order>>(
                LykkeConstants.StateBlobContainer, OrdersBlobName);
            var blobPositionsTask = _blobRepository.ReadWithTimestampAsync<List<Position>>(
                LykkeConstants.StateBlobContainer, PositionsBlobName);
            var (blobOrders, blobOrdersTimestamp) = blobOrdersTask.GetAwaiter().GetResult();
            var (blobPositions, blobPositionsTimestamp) = blobPositionsTask.GetAwaiter().GetResult();
            
            _log.WriteInfo(nameof(OrderCacheManager), nameof(InferInitDataFromBlobAndHistory),
                $"Finish reading data from blob, there are [{blobOrders.Count}] orders, [{blobPositions.Count}] positions. Start checking historical data.");
            
            var orderSnapshotsTask = _ordersHistoryRepository.GetLastSnapshot(blobOrdersTimestamp);
            var positionSnapshotsTask = _positionsHistoryRepository.GetLastSnapshot(blobPositionsTimestamp);
            var orderSnapshots = orderSnapshotsTask.GetAwaiter().GetResult().Select(OrderHistory.Create).ToList();
            PreProcess(orderSnapshots);
            var positionSnapshots = positionSnapshotsTask.GetAwaiter().GetResult();
            
            _log.WriteInfo(nameof(OrderCacheManager), nameof(InferInitDataFromBlobAndHistory),
                $"Finish reading historical data. #{orderSnapshots.Count} order history items since [{blobOrdersTimestamp:s}], #{positionSnapshots.Count} position history items since [{blobPositionsTimestamp:s}].");

            var (ordersResult, orderIdsChangedFromHistory) = MapOrders(blobOrders.ToDictionary(x => x.Id, x => x), 
                orderSnapshots.ToDictionary(x => x.Id, x => x));
            var (positionsResult, positionIdsChangedFromHistory) = MapPositions(
                blobPositions.ToDictionary(x => x.Id, x => x), positionSnapshots.ToDictionary(x => x.Id, x => x));
            
            RefreshRelated(ordersResult.ToDictionary(x => x.Id), positionsResult.ToDictionary(x => x.Id), orderSnapshots);
            ApplyExpirationDateFix(ordersResult);

            _log.WriteInfo(nameof(OrderCacheManager), nameof(InferInitDataFromBlobAndHistory),
                $"Initializing cache with [{ordersResult.Count}] orders and [{positionsResult.Count}] positions.");

            _orderCache.InitOrders(ordersResult, positionsResult);
            
            if (orderIdsChangedFromHistory.Any() || positionIdsChangedFromHistory.Any())
            {
                _log.WriteInfo(nameof(OrderCacheManager), nameof(InferInitDataFromBlobAndHistory),
                    (orderIdsChangedFromHistory.Any() ? $"Some orders state was different from history: [{string.Join(",", orderIdsChangedFromHistory)}]. " : string.Empty)
                    + (positionIdsChangedFromHistory.Any() ? $"Some positions state was different from history: [{string.Join(",", positionIdsChangedFromHistory)}]. " : string.Empty)
                    + "Dumping merged order and position data to the blob."
                );

                if (orderIdsChangedFromHistory.Any())
                {
                    DumpOrdersToRepository().Wait();
                }

                if (positionIdsChangedFromHistory.Any())
                {
                    DumpPositionsToRepository().Wait();
                }
                
                _log.WriteInfo(nameof(OrderCacheManager), nameof(InferInitDataFromBlobAndHistory),
                     "Finished dumping merged order and position data to the blob."
                ); 
            }

            return (ordersResult, positionsResult);
        }

        /// <summary>
        /// Method is added in order to fix wrong value of expiration date previously being passed by FE
        /// https://lykke-snow.atlassian.net/browse/BC-2375 
        /// </summary>
        /// <param name="orders"></param>
        private void ApplyExpirationDateFix(List<Order> orders)
        {
            var noonTime = new TimeSpan(12, 0 ,0);
            
            foreach (var order in orders)
            {
                if (!order.Validity.HasValue)
                    continue;

                var timeOfValidityDay = order.Validity.Value.TimeOfDay;
                
                // if we already fixed that validity datetime the time portion will be cut
                if (timeOfValidityDay.TotalSeconds == 0)
                    continue;

                var newDateTime = timeOfValidityDay >= noonTime
                    ? order.Validity.Value.AddDays(1)
                    : order.Validity.Value;
                
                order.FixValidity(newDateTime.Date);
            }
        }

        private void PreProcess(List<OrderHistory> orderHistories)
        {
            foreach (var orderHistory in orderHistories)
            {
                if (orderHistory.Status == OrderStatus.Placed)
                {
                    orderHistory.Status = OrderStatus.Inactive;
                }
            }
        }

        private static (List<Order> orders, List<string> orderIdsChangedFromHistory) MapOrders(
            Dictionary<string, Order> blobOrders, IReadOnlyDictionary<string, OrderHistory> orderSnapshots)
        {
            if (!orderSnapshots.Any())
            {
                return (blobOrders.Values.ToList(), new List<string>());
            }
        
            var changedIds = new List<string>();
            var result = new List<Order>();

            foreach (var (id, order) in blobOrders)
            {
                if (orderSnapshots.TryGetValue(id, out var orderHistory)
                    && order.Merge(orderHistory))
                {
                    if (OrderTerminalStatuses.Contains(orderHistory.Status))
                    {
                        continue;
                    }
                    
                    changedIds.Add(id);
                }

                result.Add(order);
            }

            foreach (var (id, orderHistory) in orderSnapshots
                .Where(x => !blobOrders.Keys.Contains(x.Key) && OrderTerminalStatuses.All(ts => ts != x.Value.Status)))
            {
                changedIds.Add(id);
                result.Add(orderHistory.FromHistory());
            }

            return (result, changedIds);
        }

        private static (List<Position> positions, List<string> positionIdsChangedFromHistory) MapPositions(
            Dictionary<string, Position> blobPositions, IReadOnlyDictionary<string, IPositionHistory> positionSnapshots)
        {
            if (!positionSnapshots.Any())
            {
                return (blobPositions.Values.ToList(), new List<string>());
            }

            var changedIds = new List<string>();
            var result = new List<Position>();

            foreach (var (id, position) in blobPositions)
            {
                if (positionSnapshots.TryGetValue(id, out var positionHistory)
                    && position.Merge(positionHistory))
                {
                    if (positionHistory.HistoryType == PositionTerminalStatus)
                    {
                        continue;
                    }

                    changedIds.Add(id);
                }
                result.Add(position);
            }

            foreach (var (id, positionHistory) in positionSnapshots
                .Where(x => !blobPositions.Keys.Contains(x.Key) && x.Value.HistoryType != PositionTerminalStatus))
            {
                changedIds.Add(id);
                result.Add(positionHistory.FromHistory());
            }

            return (result, changedIds);
        }

        private static void RefreshRelated(Dictionary<string, Order> orders, Dictionary<string, Position> positions,
            IEnumerable<OrderHistory> ordersFromHistory)
        {
            foreach (var orderHistory in ordersFromHistory)
            {
                if (!string.IsNullOrEmpty(orderHistory.ParentOrderId)
                    && orders.TryGetValue(orderHistory.ParentOrderId, out var order))
                {
                    if (OrderTerminalStatuses.Contains(orderHistory.Status))
                    {
                        order.RemoveRelatedOrder(orderHistory.Id);
                    }
                    else
                    {
                        order.AddRelatedOrder(orderHistory.FromHistory());
                    }
                }

                if (!string.IsNullOrEmpty(orderHistory.PositionId)
                    && positions.TryGetValue(orderHistory.PositionId, out var position))
                {
                    if (OrderTerminalStatuses.Contains(orderHistory.Status))
                    {
                        position.RemoveRelatedOrder(orderHistory.Id);
                    }
                    else
                    {
                        position.AddRelatedOrder(orderHistory.FromHistory());
                    }
                }
            }
        }
    }
}