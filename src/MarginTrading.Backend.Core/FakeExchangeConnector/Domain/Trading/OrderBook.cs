using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core.FakeExchangeConnector.Caches;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Core.FakeExchangeConnector.Domain.Trading
{
    public sealed class OrderBook : IKeyedObject, ICloneable
    {
        public OrderBook(string source, string assetPairId, IReadOnlyCollection<VolumePrice> asks, IReadOnlyCollection<VolumePrice> bids, DateTime timestamp)
        {
            Source = source;
            AssetPairId = assetPairId;
            Asks = asks;
            Bids = bids;
            Timestamp = timestamp;
        }

        [JsonProperty("source")]
        public string Source { get; }

        [JsonProperty("asset")]
        public string AssetPairId { get; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; }

        [JsonProperty("asks")]
        public IReadOnlyCollection<VolumePrice> Asks { get; }

        [JsonProperty("bids")]
        public IReadOnlyCollection<VolumePrice> Bids { get; }

        [JsonIgnore]
        public string Key => $"{Source}_{AssetPairId}";

        public object Clone()
        {
            return new OrderBook(Source, AssetPairId, Asks, Bids, Timestamp);
        }
    }

    public sealed class VolumePrice
    {
        public VolumePrice(decimal price, decimal volume)
        {
            Price =  price;
            Volume = Math.Abs(volume);
        }

        [JsonProperty("price")]
        public decimal Price { get; }

        [JsonProperty("volume")]
        public decimal Volume { get; }

    }
}
