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

namespace MarginTrading.Backend.Services
{
    public class OrderCacheManager : TimerPeriod
    {
        private readonly OrdersCache _orderCache;
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly IOrdersHistoryRepository _ordersHistoryRepository;
        private readonly IPositionsHistoryRepository _positionsHistoryRepository;
        private readonly ICfdCalculatorService _cfdCalculatorService;
        private readonly ILog _log;
        public const string OrdersBlobName= "orders";
        public const string PositionsBlobName= "positions";

        public OrderCacheManager(OrdersCache orderCache,
            IMarginTradingBlobRepository blobRepository,
            IOrdersHistoryRepository ordersHistoryRepository,
            IPositionsHistoryRepository positionsHistoryRepository,
            ICfdCalculatorService cfdCalculatorService,
            MarginTradingSettings marginTradingSettings,
            ILog log) 
            : base(nameof(OrderCacheManager), marginTradingSettings.BlobPersistence.OrdersDumpPeriodMilliseconds, log)
        {
            _orderCache = orderCache;
            _blobRepository = blobRepository;
            _ordersHistoryRepository = ordersHistoryRepository;
            _positionsHistoryRepository = positionsHistoryRepository;
            _cfdCalculatorService = cfdCalculatorService;
            _log = log;
        }

        public override void Start()
        {
            InferInitDataFromBlobAndHistory();

            base.Start();
        }

        public override async Task Execute()
        {
            await DumpToRepository();
        }

        public override void Stop()
        {
            DumpToRepository().Wait();
            base.Stop();
        }

        private async Task DumpToRepository()
        {
            try
            {
                var orders = _orderCache.GetAllOrders();

                if (orders != null)
                {
                    await _blobRepository.Write(LykkeConstants.StateBlobContainer, OrdersBlobName, orders);
                }
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(OrdersCache), "Save orders", "", ex);
            }
            
            try
            {
                var positions = _orderCache.GetPositions();

                if (positions != null)
                {
                    await _blobRepository.Write(LykkeConstants.StateBlobContainer, PositionsBlobName, positions);
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
            var orderSnapshots = orderSnapshotsTask.GetAwaiter().GetResult();
            var positionSnapshots = positionSnapshotsTask.GetAwaiter().GetResult();
            
            _log.WriteInfo(nameof(OrderCacheManager), nameof(InferInitDataFromBlobAndHistory),
                $"Finish reading historical data. #{orderSnapshots.Count} order history items since [{blobOrdersTimestamp:s}], #{positionSnapshots.Count} position history items since [{blobPositionsTimestamp:s}].");

            var (ordersResult, orderIdsChangedFromHistory) = MapOrders(blobOrders.ToDictionary(x => x.Id, x => x), 
                orderSnapshots.ToDictionary(x => x.Id, x => x));
            var (positionsResult, positionIdsChangedFromHistory) = MapPositions(
                blobPositions.ToDictionary(x => x.Id, x => x), positionSnapshots.ToDictionary(x => x.Id, x => x));

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
                
                DumpToRepository().GetAwaiter().GetResult();
                
                _log.WriteInfo(nameof(OrderCacheManager), nameof(InferInitDataFromBlobAndHistory),
                     "Finished dumping merged order and position data to the blob."
                ); 
            }

            return (ordersResult, positionsResult);
        }

        private static (List<Order> orders, List<string> orderIdsChangedFromHistory) MapOrders(
            Dictionary<string, Order> blobOrders, IReadOnlyDictionary<string, IOrderHistory> orderSnapshots)
        {
            var changedIds = new List<string>();
            var result = new List<Order>();

            foreach (var (id, order) in blobOrders)
            {
                if (orderSnapshots.TryGetValue(id, out var orderHistory)
                    && order.Merge(orderHistory))
                {
                    changedIds.Add(id);
                }
                result.Add(order);
            }

            foreach (var (id, orderHistory) in orderSnapshots.Where(x => !blobOrders.Keys.Contains(x.Key)))
            {
                changedIds.Add(id);
                result.Add(orderHistory.FromHistory());
            }

            return (result, changedIds);
        }

        private static (List<Position> positions, List<string> positionIdsChangedFromHistory) MapPositions(
            Dictionary<string, Position> blobPositions, IReadOnlyDictionary<string, IPositionHistory> positionSnapshots)
        {
            var changedIds = new List<string>();
            var result = new List<Position>();

            foreach (var (id, position) in blobPositions)
            {
                if (positionSnapshots.TryGetValue(id, out var positionHistory)
                    && position.Merge(positionHistory))
                {
                    changedIds.Add(id);
                }
                result.Add(position);
            }

            foreach (var (id, positionHistory) in positionSnapshots.Where(x => !blobPositions.Keys.Contains(x.Key)))
            {
                changedIds.Add(id);
                result.Add(positionHistory.FromHistory());
            }

            return (result, changedIds);
        }
    }
}