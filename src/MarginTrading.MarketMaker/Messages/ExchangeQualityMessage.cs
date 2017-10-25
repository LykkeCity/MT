using MarginTrading.MarketMaker.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.MarketMaker.Messages
{
    public class ExchangeQualityMessage
    {
        public string Exchange { get; set; }
        public decimal HedgingPreference { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ExchangeErrorState? Error { get; set; }

        public bool OrderbookReceived { get; set; }
    }
}
