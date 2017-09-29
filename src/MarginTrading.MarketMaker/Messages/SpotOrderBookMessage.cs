using System;
using System.Collections.Generic;

namespace MarginTrading.MarketMaker.Messages
{
    /// <summary>
    /// OrderBook message for an asset pair
    /// </summary>
    public class SpotOrderbookMessage
    {
        public string AssetPair { get; set; }
        public bool IsBuy { get; set; }
        public DateTime Timestamp { get; set; }
        public List<VolumePrice> Prices { get; set; } = new List<VolumePrice>();
    }
}
