using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Core;
using MarginTrading.Core.Assets;
using MarginTrading.Core.MarketMakerFeed;

namespace MarginTrading.Services
{
    public class MarketMakerService : IFeedConsumer
    {
        private readonly IMatchingEngine _matchingEngine;
        private readonly IAssetDayOffService _assetDayOffService;
        private readonly IAccountAssetsCacheService _assetsCacheService;
        private const string MarketMakerId = "marketMaker1";

        public MarketMakerService(IMatchingEngine matchingEngine,
            IAssetDayOffService assetDayOffService,
            IAccountAssetsCacheService assetsCacheService)
        {
            _matchingEngine = matchingEngine;
            _assetDayOffService = assetDayOffService;
            _assetsCacheService = assetsCacheService;
        }

        public void ConsumeFeed(IAssetPairRate feedData)
        {
            //if no asset pair ID in trading conditions, no need to process price
            if (!_assetsCacheService.IsInstrumentSupported(feedData.AssetPairId))
                return;

            var direction = feedData.IsBuy ? OrderDirection.Buy : OrderDirection.Sell;
            LimitOrder order = null;

            if (!_assetDayOffService.IsDayOff(feedData.AssetPairId))
            {
                var volume = feedData.IsBuy ? 1000000 : -1000000;
                order = new LimitOrder
                {
                    Id = Guid.NewGuid().ToString("N"),
                    MarketMakerId = MarketMakerId,
                    CreateDate = DateTime.UtcNow,
                    Instrument = feedData.AssetPairId,
                    Price = feedData.Price,
                    Volume = volume
                };
            }

            var model = new SetOrderModel
            {
                DeleteByInstrumentsBuy =
                    direction == OrderDirection.Buy ? new[] {feedData.AssetPairId} : Array.Empty<string>(),
                DeleteByInstrumentsSell =
                    direction == OrderDirection.Sell ? new[] {feedData.AssetPairId} : Array.Empty<string>(),
                MarketMakerId = MarketMakerId,
                OrdersToAdd = order != null ? new[] {order} : Array.Empty<LimitOrder>()
            };

            _matchingEngine.SetOrders(model);
        }

        public async Task ShutdownApplication()
        {
            await Task.Run(() =>
            {
                Console.WriteLine("Processing before shutdown");
            });
        }
    }
}
