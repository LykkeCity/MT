using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Infrastructure;
using MoreLinq;

namespace MarginTrading.Backend.Services.Migrations
{
    public sealed class PendingMarginInstrumentMigration : IMigration
    {
        public int Version => 1;
        
        private readonly IAssetPairsCache _assetPairsCache;

        private readonly OrdersCache _orderCache;
        
        private readonly IContextFactory _contextFactory;
        
        public PendingMarginInstrumentMigration(
            IAssetPairsCache assetPairsCache,
            OrdersCache orderCache,
            IContextFactory contextFactory) 
        {
            _assetPairsCache = assetPairsCache;

            _orderCache = orderCache;

            _contextFactory = contextFactory;
        }

        public Task Invoke()
        {
            using (_contextFactory.GetWriteSyncContext($"{nameof(PendingMarginInstrumentMigration)}.{nameof(Invoke)}"))
            {
                //open orders from cache
                var allOrders = _orderCache.GetAll().ToList();
                var pendingOrders = _orderCache.GetPending().Where(x => string.IsNullOrEmpty(x.MarginCalcInstrument)).ToList();
                if (!pendingOrders.Any())
                    return Task.CompletedTask;
                
                foreach (var order in pendingOrders)
                {
                    HandleOrder(order);
                }
                
                //reinit orders cache with modified data
                _orderCache.InitOrders(allOrders);
            }

            return Task.CompletedTask;
        }

        private void HandleOrder(Order order)
        {
            if (_assetPairsCache.TryGetAssetPairQuoteSubst(order.AccountAssetId, order.Instrument,
                order.LegalEntity, out var substAssetPair))
            {
                order.MarginCalcInstrument = substAssetPair.Id;
            }
        }
    }
}