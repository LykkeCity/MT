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
        private const string MarketMakerId = "marketMaker1";

        public MarketMakerService(IMatchingEngine matchingEngine,
            IAssetDayOffService assetDayOffService)
        {
            _matchingEngine = matchingEngine;
            _assetDayOffService = assetDayOffService;
        }

        public void ConsumeFeed(IAssetPairRate[] feedDatas)
        {
            var orders = new List<LimitOrder>();
            foreach (var assetPairRate in feedDatas)
            {
                var volume = _assetDayOffService.IsDayOff(assetPairRate.AssetPairId)
                    ? 0
                    : assetPairRate.IsBuy
                        ? 1000000
                        : -1000000;

                orders.Add(new LimitOrder
                {
                    Id = Guid.NewGuid().ToString("N"),
                    MarketMakerId = MarketMakerId,
                    CreateDate = DateTime.UtcNow,
                    Instrument = assetPairRate.AssetPairId,
                    Price = assetPairRate.Price,
                    Volume = volume
                });
            }

            var order = orders[0];

            var model = new SetOrderModel
            {
                DeleteByInstrumentsBuy =
                    order.GetOrderType() == OrderDirection.Buy ? new[] {order.Instrument} : Array.Empty<string>(),
                DeleteByInstrumentsSell =
                    order.GetOrderType() == OrderDirection.Sell ? new[] {order.Instrument} : Array.Empty<string>(),
                MarketMakerId = MarketMakerId,
                OrdersToAdd = orders.ToArray()
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
