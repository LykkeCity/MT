using System;
using Newtonsoft.Json;

namespace MarginTrading.MarketMaker.Messages
{
    /// <summary>
    /// Info about best bid and ask for an asset
    /// </summary>
    public class BestBidAskMessage
    {
        /// <summary>
        /// Source - in our case it's "ICM"
        /// </summary>
        [JsonProperty("source")]
        public string Source { get; set; }

        /// <summary>
        /// This actualy is an asset pair id
        /// </summary>
        [JsonProperty("asset")]
        public string Asset { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("bestAsk")]
        public double? BestAsk { get; set; }

        [JsonProperty("bestBid")]
        public double? BestBid { get; set; }
    }
}