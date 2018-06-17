using System;
using System.Collections.Generic;
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
        private readonly ILog _log;
        private const string OrdersBlobName= "orders";
        private const string PositionsBlobName= "positions";

        public OrderCacheManager(OrdersCache orderCache,
            IMarginTradingBlobRepository marginTradingBlobRepository,
            MarginTradingSettings marginTradingSettings,
            ILog log) 
            : base(nameof(OrderCacheManager), marginTradingSettings.BlobPersistence.OrdersDumpPeriodMilliseconds, log)
        {
            _orderCache = orderCache;
            _marginTradingBlobRepository = marginTradingBlobRepository;
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
    }
}