using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IQuoteCacheService _quoteCache;
        private const string MarketMakerId = "marketMaker1";
        private static Dictionary<string, IAssetPairRate> _pendingBuyRates = new Dictionary<string, IAssetPairRate>();
        private static Dictionary<string, IAssetPairRate> _pendingSellRates = new Dictionary<string, IAssetPairRate>();

        public MarketMakerService(IMatchingEngine matchingEngine,
            IAssetDayOffService assetDayOffService,
            IAccountAssetsCacheService assetsCacheService,
            IQuoteCacheService quoteCache)
        {
            _matchingEngine = matchingEngine;
            _assetDayOffService = assetDayOffService;
            _assetsCacheService = assetsCacheService;
            _quoteCache = quoteCache;
        }

        public void ConsumeFeed(IAssetPairRate feedData)
        {
            //if no asset pair ID in trading conditions, no need to process price
            if (!_assetsCacheService.IsInstrumentSupported(feedData.AssetPairId))
                return;

            var orders = CreateLimitOrders(feedData);

            var model = new SetOrderModel
            {
                DeleteByInstrumentsBuy =
                    feedData.IsBuy ? new[] {feedData.AssetPairId} : Array.Empty<string>(),
                DeleteByInstrumentsSell =
                    !feedData.IsBuy ? new[] {feedData.AssetPairId} : Array.Empty<string>(),
                MarketMakerId = MarketMakerId,
                OrdersToAdd = orders ?? Array.Empty<LimitOrder>()
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

        private LimitOrder[] CreateLimitOrders(IAssetPairRate feedData)
        {
            if (_assetDayOffService.IsDayOff(feedData.AssetPairId))
            {
                return null;
            }

            InstrumentBidAskPair bestBidAsk;
            IAssetPairRate pendingFeed = null;

            if (_quoteCache.TryGetQuoteById(feedData.AssetPairId, out bestBidAsk))
            {
                if (feedData.IsBuy)
                {
                    var bid = feedData.Price;
                    var ask = bestBidAsk.Ask;
                    _pendingSellRates.TryGetValue(feedData.AssetPairId, out pendingFeed);

                    if (bid >= ask)
                    {
                        if (pendingFeed != null)
                        {
                            if (bid >= pendingFeed.Price)
                            {
                                _pendingBuyRates[feedData.AssetPairId] = feedData;
                                return null;
                            }
                        }
                        else
                        {
                            _pendingBuyRates[feedData.AssetPairId] = feedData;
                            return null;
                        }
                    }

                    if (pendingFeed != null)
                    {
                        _pendingSellRates.Remove(feedData.AssetPairId);
                    }

                }
                else
                {
                    var bid = bestBidAsk.Bid;
                    var ask = feedData.Price;
                    _pendingBuyRates.TryGetValue(feedData.AssetPairId, out pendingFeed);

                    if (bid >= ask)
                    {
                        if (pendingFeed != null)
                        {
                            if (bid >= pendingFeed.Price)
                            {
                                _pendingSellRates[feedData.AssetPairId] = feedData;
                                return null;
                            }
                        }
                        else
                        {
                            _pendingSellRates[feedData.AssetPairId] = feedData;
                            return null;
                        }
                    }

                    if (pendingFeed != null)
                    {
                        _pendingBuyRates.Remove(feedData.AssetPairId);
                    }
                }
            }

            return new[] {feedData, pendingFeed}.Where(d => d != null).Select(CreateOrder).ToArray();
        }

        private LimitOrder CreateOrder(IAssetPairRate feedData)
        {
            var volume = feedData.IsBuy ? 1000000 : -1000000;
            return new LimitOrder
            {
                Id = Guid.NewGuid().ToString("N"),
                MarketMakerId = MarketMakerId,
                CreateDate = DateTime.UtcNow,
                Instrument = feedData.AssetPairId,
                Price = feedData.Price,
                Volume = volume
            };
        }
    }
}
