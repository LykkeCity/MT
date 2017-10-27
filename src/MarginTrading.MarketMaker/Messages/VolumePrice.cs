using Newtonsoft.Json;

namespace MarginTrading.MarketMaker.Messages
{
    public class VolumePrice
    {
        [JsonProperty("volume")]
        public decimal Volume { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }
    }
}
