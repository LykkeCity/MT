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
        public ExchangeErrorState? State { get; }
        public bool OrderbookReceived { get; }

        public ExchangeQuality(string exchange, decimal preference, ExchangeErrorState? state, bool orderbookReceived)
        {
            Exchange = exchange;
            Preference = preference;
            State = state;
            OrderbookReceived = orderbookReceived;
        }
    }
}
