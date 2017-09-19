using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MarginTrading.MarketMaker.Messages
{
    /// <summary>
    /// Info about best bid and ask for an asset
    /// </summary>
    public class ExternalExchangeOrderbookMessage
    {
        /// <summary>
        /// Source
        /// </summary>
        [JsonProperty("source")]
        public string Source { get; set; }

        /// <summary>
        /// Asset pair id
        /// </summary>
        [JsonProperty("asset")]
        public string AssetPairId { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("asks")]
        public IReadOnlyList<VolumePrice> Asks { get; set; }

        [JsonProperty("bids")]
        public IReadOnlyList<VolumePrice> Bids { get; set; }
    }
}