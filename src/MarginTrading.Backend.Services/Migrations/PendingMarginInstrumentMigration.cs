using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Migrations
{
    public sealed class PendingMarginInstrumentMigration : AbstractMigration
    {
        private readonly IAssetPairsCache _assetPairsCache;

        private readonly OrdersCache _orderCache;
        
        private readonly IMarginTradingBlobRepository _marginTradingBlobRepository;
        
        public PendingMarginInstrumentMigration(
            IAssetPairsCache assetPairsCache,
            OrdersCache orderCache,
            IMarginTradingBlobRepository marginTradingBlobRepository)
        {
            _assetPairsCache = assetPairsCache;

            _orderCache = orderCache;
            
            _marginTradingBlobRepository = marginTradingBlobRepository;
        }

        public override async Task Invoke()
        {
            //open orders from cache
            foreach (var immutablePendingOrder in _orderCache.GetPending())
            {
                var order = _orderCache.GetOrderById(immutablePendingOrder.Id);
                HandleOrder(order);
            }
        }

        private void HandleOrder(Order order)
        {
            if (_assetPairsCache.TryGetAssetPairQuoteSubstWithResersed(order.AccountAssetId, order.Instrument,
                order.LegalEntity, out var substAssetPair))
            {
                order.MarginCalcInstrument = substAssetPair.Id;
            }
        }
    }
}