using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services
{
    public class OrderCacheManager : TimerPeriod
    {
        private readonly OrdersCache _orderCache;
        private readonly IMarginTradingBlobRepository _marginTradingBlobRepository;
        private readonly ICfdCalculatorService _cfdCalculatorService;
        private readonly ILog _log;
        public const string OrdersBlobName= "orders";
        public const string PositionsBlobName= "positions";

        public OrderCacheManager(OrdersCache orderCache,
            IMarginTradingBlobRepository marginTradingBlobRepository,
            ICfdCalculatorService cfdCalculatorService,
            MarginTradingSettings marginTradingSettings,
            ILog log) 
            : base(nameof(OrderCacheManager), marginTradingSettings.BlobPersistence.OrdersDumpPeriodMilliseconds, log)
        {
            _orderCache = orderCache;
            _marginTradingBlobRepository = marginTradingBlobRepository;
            _cfdCalculatorService = cfdCalculatorService;
            _log = log;
        }

        public override void Start()
        {
            var orders =
                _marginTradingBlobRepository.Read<List<Order>>(LykkeConstants.StateBlobContainer, OrdersBlobName) ??
                new List<Order>();
            var positions =
                _marginTradingBlobRepository.Read<List<Position>>(LykkeConstants.StateBlobContainer, PositionsBlobName) ??
                new List<Position>();

            MigrateFx(orders, positions);//todo remove after deployment
            
            _orderCache.InitOrders(orders, positions);

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
                    await _marginTradingBlobRepository.Write(LykkeConstants.StateBlobContainer, OrdersBlobName, orders);
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
                    await _marginTradingBlobRepository.Write(LykkeConstants.StateBlobContainer, PositionsBlobName, positions);
                }
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(OrdersCache), "Save positions", "", ex);
            }
        }

        private void MigrateFx(List<Order> orders, List<Position> positions)
        {
            foreach (var order in orders.Where(x => string.IsNullOrEmpty(x.FxAssetPairId)))
            {
                var fx = _cfdCalculatorService.GetFxAssetPairIdAndDirection(order.AccountAssetId, order.AssetPairId,
                    order.LegalEntity);

                order.FxAssetPairId = fx.id;
                order.FxToAssetPairDirection = fx.direction;
            }

            foreach (var position in positions.Where(x => string.IsNullOrEmpty(x.FxAssetPairId)))
            {
                var fx = _cfdCalculatorService.GetFxAssetPairIdAndDirection(position.AccountAssetId, position.AssetPairId,
                    position.LegalEntity);

                position.FxAssetPairId = fx.id;
                position.FxToAssetPairDirection = fx.direction;
            }
        }
    }
}