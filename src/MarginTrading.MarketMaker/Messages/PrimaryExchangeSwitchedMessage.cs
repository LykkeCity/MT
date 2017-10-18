using System.Collections.Generic;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Messages
{
    public class PrimaryExchangeSwitchedMessage
    {
        public string AssetPairId { get; }
        public ExchangeQuality NewPrimaryExchange { get; }
        public IReadOnlyCollection<ExchangeQuality> AllExchangesStates { get; }

        public PrimaryExchangeSwitchedMessage(string assetPairId, ExchangeQuality newPrimaryExchange, IReadOnlyCollection<ExchangeQuality> allExchangesStates)
        {
            AssetPairId = assetPairId;
            NewPrimaryExchange = newPrimaryExchange;
            AllExchangesStates = allExchangesStates;
        }
    }
}
