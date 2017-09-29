using System;
using System.Collections.Generic;
using JetBrains.Annotations;
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
        [JsonProperty("source"), CanBeNull]
        public string Source { get; set; }

        /// <summary>
        /// Asset pair id
        /// </summary>
        [JsonProperty("asset"), CanBeNull]
        public string AssetPairId { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("asks"), CanBeNull, ItemCanBeNull]
        public IReadOnlyList<VolumePrice> Asks { get; set; }

        [JsonProperty("bids"), CanBeNull, ItemCanBeNull]
        public IReadOnlyList<VolumePrice> Bids { get; set; }
    }
}