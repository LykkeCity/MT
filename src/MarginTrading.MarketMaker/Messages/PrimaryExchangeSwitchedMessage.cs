using System.Collections.Generic;

namespace MarginTrading.MarketMaker.Messages
{
    public class PrimaryExchangeSwitchedMessage
    {
        public string MarketMakerId { get; set; }
        public string AssetPairId { get; set; }
        public ExchangeQualityMessage NewPrimaryExchange { get; set; }
        public IReadOnlyCollection<ExchangeQualityMessage> AllExchangesStates { get; set; }
    }
}
