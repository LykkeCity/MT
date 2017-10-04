using System;
using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Core.MarketMakerFeed
{
    public interface IMarketMakerOrderBook
    {
        string AssetPair { get; }
        bool IsBuy { get; }
        DateTime Timestamp { get; }
        List<VolumePrice> Prices { get; }
    }

    public class MarketMakerOrderBook : IMarketMakerOrderBook
    {
        public string AssetPair { get; set; }
        public bool IsBuy { get; set; }
        public DateTime Timestamp { get; set; }
        public List<VolumePrice> Prices { get; set; } = new List<VolumePrice>();
    }

    public class VolumePrice
    {
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
    }

    public static class OrderBookExt
    {
        public static decimal GetPrice(this IMarketMakerOrderBook src)
        {
            return src.IsBuy
                ? src.Prices.Max(item => item.Price)
                : src.Prices.Min(item => item.Price);
        }
    }
}
