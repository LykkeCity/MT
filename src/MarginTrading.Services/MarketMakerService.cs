using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Common.Extensions;
using MarginTrading.Core;
using MarginTrading.Core.Assets;
using MarginTrading.Core.MarketMakerFeed;

namespace MarginTrading.Services
{
    public class MarketMakerService : IFeedConsumer
    {
        private static readonly Dictionary<string, IAssetPairRate> _pendingBuyRates =
            new Dictionary<string, IAssetPairRate>();

        private static readonly Dictionary<string, IAssetPairRate> _pendingSellRates =
            new Dictionary<string, IAssetPairRate>();

        private readonly IAssetDayOffService _assetDayOffService;
        private readonly IAccountAssetsCacheService _assetsCacheService;
        private readonly IMatchingEngine _matchingEngine;
        private readonly IQuoteCacheService _quoteCache;

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

        public void ConsumeFeed(MarketMakerOrderCommandsBatchMessage batch)
        {
            batch.AssetPairId.RequiredNotNullOrWhiteSpace(nameof(batch.AssetPairId));
            batch.Commands.RequiredNotNull(nameof(batch.Commands));

            //if no asset pair ID in trading conditions, no need to process price
            if (!_assetsCacheService.IsInstrumentSupported(batch.AssetPairId))
            {
                return;
            }

            var model = new SetOrderModel {MarketMakerId = batch.MarketMakerId};

            if (DeleteOrdersIfDayOff(batch, model))
            {
                return;
            }

            ConvertCommandsToOrders(batch, model);
            if (model.OrdersToAdd?.Count > 0 || model.DeleteByInstrumentsBuy?.Count > 0 ||
                model.DeleteByInstrumentsSell?.Count > 0)
            {
                _matchingEngine.SetOrders(model);
            }
        }

        public async Task ShutdownApplication()
        {
            await Task.Run(() => { Console.WriteLine("Processing before shutdown"); });
        }

        private void ConvertCommandsToOrders(MarketMakerOrderCommandsBatchMessage batch, SetOrderModel model)
        {
            var setCommands = batch.Commands.Where(c => c.CommandType == MarketMakerOrderCommandType.SetOrder && c.Direction != null).ToList();
            var directions = setCommands.Select(c => c.Direction).Distinct().ToList();
            if (directions.Count == 1)
            {
                // it's trickery time
                model.OrdersToAdd = setCommands.Select(c => CreateLimitOrders(new AssetPairRate
                    {
                        AssetPairId = batch.AssetPairId,
                        IsBuy = c.Direction.RequiredNotNull(nameof(c.Direction)) == OrderDirection.Buy,
                        Price = c.Price.RequiredNotNull(nameof(c.Price))
                    }, batch.MarketMakerId)).Where(a => a?.Length > 0).SelectMany(a => a)
                    .ToList();

                if (model.OrdersToAdd.Count > 0)
                {
                    AddOrdersToDelete(batch, model);
                    if (directions[0] == OrderDirection.Buy)
                    {
                        model.DeleteByInstrumentsSell = null;
                    }
                    else
                    {
                        model.DeleteByInstrumentsBuy = null;
                    }
                }
            }
            else
            {
                model.OrdersToAdd = setCommands
                    .Select(c => new LimitOrder
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        MarketMakerId = batch.MarketMakerId,
                        CreateDate = DateTime.UtcNow,
                        Instrument = batch.AssetPairId,
                        Price = c.Price.RequiredNotNull(nameof(c.Price)),
                        Volume = c.Direction.RequiredNotNull(nameof(c.Direction)) == OrderDirection.Buy
                            ? 1000000
                            : -1000000
                    }).ToList();

                AddOrdersToDelete(batch, model);
            }
        }

        private static void AddOrdersToDelete(MarketMakerOrderCommandsBatchMessage batch, SetOrderModel model)
        {
            var buy = new List<string>();
            var sell = new List<string>();
            foreach (var command in batch.Commands.Where(c => c.CommandType == MarketMakerOrderCommandType.DeleteOrder))
            {
                switch (command.Direction)
                {
                    case OrderDirection.Buy:
                        buy.Add(batch.AssetPairId);
                        break;
                    case OrderDirection.Sell:
                        sell.Add(batch.AssetPairId);
                        break;
                    case null:
                        buy.Add(batch.AssetPairId);
                        sell.Add(batch.AssetPairId);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(command.Direction), command.Direction,
                            "This order direction is not supported");
                }
            }

            model.DeleteByInstrumentsBuy = buy;
            model.DeleteByInstrumentsSell = sell;
        }

        private bool DeleteOrdersIfDayOff(MarketMakerOrderCommandsBatchMessage batch, SetOrderModel model)
        {
            if (_assetDayOffService.IsDayOff(batch.AssetPairId))
            {
                model.DeleteByInstrumentsBuy = new[] {batch.AssetPairId};
                model.DeleteByInstrumentsSell = new[] {batch.AssetPairId};
                _matchingEngine.SetOrders(model);
                return true;
            }

            return false;
        }

        [CanBeNull]
        private LimitOrder[] CreateLimitOrders(IAssetPairRate feedData, string marketMakerId)
        {
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

            return new[] {feedData, pendingFeed}.Where(d => d != null).Select(rate => CreateOrder(rate, marketMakerId)).ToArray();
        }

        private LimitOrder CreateOrder(IAssetPairRate feedData, string marketMakerId)
        {
            var volume = feedData.IsBuy ? 1000000 : -1000000;
            return new LimitOrder
            {
                Id = Guid.NewGuid().ToString("N"),
                MarketMakerId = marketMakerId,
                CreateDate = DateTime.UtcNow,
                Instrument = feedData.AssetPairId,
                Price = feedData.Price,
                Volume = volume
            };
        }
    }
}