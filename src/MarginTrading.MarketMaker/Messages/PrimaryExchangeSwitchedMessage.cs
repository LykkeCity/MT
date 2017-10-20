using System.Collections.Generic;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Messages
{
    public class PrimaryExchangeSwitchedMessage
    {
        public string MarketMakerId { get; set; }
        public string AssetPairId { get; set; }
        public ExchangeQuality NewPrimaryExchange { get; set; }
        public IReadOnlyCollection<ExchangeQuality> AllExchangesStates { get; set; }
    }
}
