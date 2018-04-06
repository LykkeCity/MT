using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services
{
    public class OrderCacheManager : TimerPeriod
    {
        private readonly OrdersCache _orderCache;
        private readonly IMarginTradingBlobRepository _marginTradingBlobRepository;
        private readonly ILog _log;
        private readonly IAccountsCacheService _accountsCacheService;
        private const string BlobName= "orders";

        public OrderCacheManager(OrdersCache orderCache,
            IMarginTradingBlobRepository marginTradingBlobRepository,
            ILog log, IAccountsCacheService accountsCacheService) 
            : base(nameof(OrderCacheManager), 5000, log)
        {
            _orderCache = orderCache;
            _marginTradingBlobRepository = marginTradingBlobRepository;
            _log = log;
            _accountsCacheService = accountsCacheService;
        }

        public override void Start()
        {
            var orders = _marginTradingBlobRepository.Read<List<Order>>(LykkeConstants.StateBlobContainer, BlobName) ?? new List<Order>();
            
            orders.ForEach(o =>
            {
                // migrate orders to add LegalEntity field
                // todo: can be removed once published to prod
                if (o.LegalEntity == null)
                    o.LegalEntity = _accountsCacheService.Get(o.ClientId, o.AccountId).LegalEntity;
            });

            _orderCache.InitOrders(orders);

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
                var orders = _orderCache.GetAll();

                if (orders != null)
                {
                    await _marginTradingBlobRepository.Write(LykkeConstants.StateBlobContainer, BlobName, orders);
                }
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(OrdersCache), "Save orders", "", ex);
            }
        }
    }
}