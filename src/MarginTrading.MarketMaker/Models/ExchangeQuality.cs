using MarginTrading.MarketMaker.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.MarketMaker.Models
{
    public class ExchangeQuality
    {
        public string Exchange { get; }
        public decimal Preference { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ExchangeErrorState? Error { get; }
        public bool OrderbookReceived { get; }

        public ExchangeQuality(string exchange, decimal preference, ExchangeErrorState? error, bool orderbookReceived)
        {
            Exchange = exchange;
            Preference = preference;
            Error = error;
            OrderbookReceived = orderbookReceived;
        }
    }
}
