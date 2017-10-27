using System;
using System.Collections.Immutable;

namespace MarginTrading.MarketMaker.Models
{
    public class ExternalOrderbook : Orderbook
    {
        public string AssetPairId { get; }
        public string ExchangeName { get; }
        public DateTime LastUpdatedTime { get; }

        public ExternalOrderbook(string assetPairId, string exchangeName, DateTime lastUpdatedTime,
            ImmutableArray<OrderbookPosition> bids, ImmutableArray<OrderbookPosition> asks)
            : base(bids, asks)
        {
            LastUpdatedTime = lastUpdatedTime;
            AssetPairId = assetPairId;
            ExchangeName = exchangeName;
        }
    }
}