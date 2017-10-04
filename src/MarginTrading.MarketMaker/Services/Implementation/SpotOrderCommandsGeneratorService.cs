using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.Messages;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    internal class SpotOrderCommandsGeneratorService : ISpotOrderCommandsGeneratorService
    {
        private static readonly Dictionary<string, AssetPairRate> _pendingBuyRates =
            new Dictionary<string, AssetPairRate>();

        private static readonly Dictionary<string, AssetPairRate> _pendingSellRates =
            new Dictionary<string, AssetPairRate>();

        private readonly Dictionary<string, AssetPairBidAsk> _quotes =
            new Dictionary<string, AssetPairBidAsk>();

        private readonly ConcurrentDictionary<string, object> _locksByAssetPairId =
            new ConcurrentDictionary<string, object>();

        public IReadOnlyList<OrderCommand> GenerateOrderCommands(string assetPairId, bool isBuy, decimal newBestPrice, decimal ordersVolume)
        {
            lock (_locksByAssetPairId.GetOrAdd(assetPairId, k => new object()))
            {
                return FixNegativeSpreadAndCreateOrderCommands(ordersVolume, assetPairId, isBuy, newBestPrice)
                       ?? Array.Empty<OrderCommand>();
            }
        }

        [CanBeNull]
        private IReadOnlyList<OrderCommand> FixNegativeSpreadAndCreateOrderCommands(decimal ordersVolume, string assetPairId, bool isBuy, decimal newBestPrice)
        {
            AssetPairRate feedData = new AssetPairRate
            {
                BestPrice = newBestPrice,
                AssetPairId = assetPairId,
                IsBuy = isBuy,
            };

            AssetPairRate pendingFeedData;

            decimal bid;
            decimal ask;
            _quotes.TryGetValue(assetPairId, out var bestBidAsk);

            if (isBuy)
            {
                bid = newBestPrice;
                ask = bestBidAsk?.Ask ?? decimal.MaxValue;
                _pendingSellRates.TryGetValue(assetPairId, out pendingFeedData);

                if (bid >= ask)
                {
                    if (pendingFeedData != null)
                    {
                        if (bid >= pendingFeedData.BestPrice)
                        {
                            _pendingBuyRates[assetPairId] = feedData;
                            return null;
                        }
                    }
                    else
                    {
                        _pendingBuyRates[assetPairId] = feedData;
                        return null;
                    }
                }

                if (pendingFeedData != null)
                {
                    _pendingSellRates.Remove(assetPairId);
                }
            }
            else
            {
                bid = bestBidAsk?.Bid ?? decimal.MinValue;
                ask = newBestPrice;
                _pendingBuyRates.TryGetValue(assetPairId, out pendingFeedData);

                if (bid >= ask)
                {
                    if (pendingFeedData != null)
                    {
                        if (bid >= pendingFeedData.BestPrice)
                        {
                            _pendingSellRates[assetPairId] = feedData;
                            return null;
                        }
                    }
                    else
                    {
                        _pendingSellRates[assetPairId] = feedData;
                        return null;
                    }
                }

                if (pendingFeedData != null)
                {
                    _pendingBuyRates.Remove(assetPairId);
                }
            }

            _quotes[assetPairId] = new AssetPairBidAsk
            {
                Ask = ask,
                Bid = bid,
            };

            var orders = new[] {feedData, pendingFeedData}.Where(d => d != null)
                .Select(rate => CreateCommand(rate, ordersVolume)).ToList();


            AppendDeleteCommands(orders);

            return orders;
        }

        private static void AppendDeleteCommands(List<OrderCommand> orders)
        {
            var orderDirections = orders.Where(o => o.Direction != null).Select(o => o.Direction).Distinct().ToList();
            foreach (var orderDirection in orderDirections)
            {
                orders.Add(new OrderCommand
                {
                    CommandType = OrderCommandTypeEnum.DeleteOrder,
                    Direction = orderDirection,
                });
            }
        }

        private static OrderCommand CreateCommand(AssetPairRate feedData, decimal volume)
        {
            return new OrderCommand
            {
                Price = feedData.BestPrice,
                Volume = volume,
                CommandType = OrderCommandTypeEnum.SetOrder,
                Direction = feedData.IsBuy ? OrderDirectionEnum.Buy : OrderDirectionEnum.Sell,
            };
        }

        private class AssetPairBidAsk
        {
            public decimal Bid { get; set; }
            public decimal Ask { get; set; }
        }

        private class AssetPairRate
        {
            public string AssetPairId { get; set; }
            public bool IsBuy { get; set; }
            public decimal BestPrice { get; set; }
        }
    }
}
