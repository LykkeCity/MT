using System.Collections.Immutable;

namespace MarginTrading.MarketMaker.Services
{
    public interface IDisabledOrderbooksService
    {
        ImmutableHashSet<string> GetDisabledExchanges(string assetPairId);
        void Disable(string assetPairId, ImmutableHashSet<string> exchanges);
    }
}