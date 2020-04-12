// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.Orderbooks;

namespace MarginTrading.Backend.Core.Extensions
{
    public static class ExternalOrderBookExtensions
    {
        public static object ToContextData(this ExternalOrderBook orderbook)
        {
            if (orderbook == null) return null;

            return new
            {
                assetPairId = orderbook.AssetPairId,
                timestamp = orderbook.Timestamp,
                bestAsk = orderbook.Asks[0],
                bestBid = orderbook.Bids[0],
                exchangeName = orderbook.ExchangeName
            };
        }
    }
}