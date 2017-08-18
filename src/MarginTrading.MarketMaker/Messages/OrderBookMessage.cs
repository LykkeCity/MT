using System;
using System.Collections.Generic;
using System.Text;

namespace MarginTrading.MarketMaker.Messages
{
    /// <summary>
    /// OrderBook message for an asset pair
    /// </summary>
    public class OrderBookMessage
    {
        public string AssetPair { get; set; }
        public bool IsBuy { get; set; }
        public DateTime Timestamp { get; set; }
        public List<VolumePrice> Prices { get; set; } = new List<VolumePrice>();
        public double BestVolume => Prices[0].Volume;
        public double BestPrice => Prices[0].Price;

        public class VolumePrice
        {
            public double Volume { get; set; }
            public double Price { get; set; }
        }
    }
}
