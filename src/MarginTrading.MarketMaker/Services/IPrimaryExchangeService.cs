using System.Collections.Immutable;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.Enums;

namespace MarginTrading.MarketMaker.Services
{
    public interface IPrimaryExchangeService
    {
        [CanBeNull]
        string GetPrimaryExchange(string assetPairId, ImmutableDictionary<string, ExchangeErrorState> exchanges);
    }
}