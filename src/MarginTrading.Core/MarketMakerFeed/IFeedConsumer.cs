using System;
using System.Threading.Tasks;

namespace MarginTrading.Core.MarketMakerFeed
{
    public interface IFeedConsumer
    {
        void ConsumeFeed(MarketMakerOrderCommandsBatchMessage batch);
    }

    public interface IAssetPairRate
    {
        string AssetPairId { get; }
        DateTime DateTime { get; }
        bool IsBuy { get; }
        decimal Price { get; }
        decimal Volume { get; }
    }

    
    public class AssetPairRate : IAssetPairRate
    {
        public string AssetPairId { get; set; }
        public DateTime DateTime { get; set; }
        public bool IsBuy { get; set; }
        public decimal Price { get; set; }
        public decimal Volume { get; set; }

        public static IAssetPairRate Create(IMarketMakerOrderBook src)
        {
            return new AssetPairRate
            {
                AssetPairId = src.AssetPair,
                DateTime = src.Timestamp,
                IsBuy = src.IsBuy,
                Price = src.Prices[0].Price,
                Volume = src.Prices[0].Volume
            };
        }
    }
}
