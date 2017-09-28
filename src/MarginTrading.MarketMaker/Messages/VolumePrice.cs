using Newtonsoft.Json;

namespace MarginTrading.MarketMaker.Messages
{
    public class VolumePrice
    {
        [JsonProperty("volume")]
        public double Volume { get; set; }

        [JsonProperty("price")]
        public double Price { get; set; }

    }
}
