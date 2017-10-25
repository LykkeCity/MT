using MarginTrading.MarketMaker.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.MarketMaker.Models
{
    public class ExchangeQuality
    {
        public string Exchange { get; }
        public decimal HedgingPreference { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ExchangeErrorState? Error { get; }
        public bool OrderbookReceived { get; }

        public ExchangeQuality(string exchange, decimal hedgingPreference, ExchangeErrorState? error, bool orderbookReceived)
        {
            Exchange = exchange;
            HedgingPreference = hedgingPreference;
            Error = error;
            OrderbookReceived = orderbookReceived;
        }
    }
}
