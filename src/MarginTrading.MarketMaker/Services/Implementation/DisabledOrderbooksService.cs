using System;
using System.Collections.Immutable;
using MarginTrading.MarketMaker.HelperServices.Implemetation;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class DisabledOrderbooksService : IDisabledOrderbooksService
    {
        private readonly ReadWriteLockedDictionary<string, ImmutableHashSet<string>> _disabledOrderbooks =
            new ReadWriteLockedDictionary<string, ImmutableHashSet<string>>();

        public ImmutableHashSet<string> GetDisabledExchanges(string assetPairId)
        {
            return _disabledOrderbooks.GetOrDefault(assetPairId, k => ImmutableHashSet<string>.Empty) ?? throw new Exception("wtf");
        }

        public void Disable(string assetPairId, ImmutableHashSet<string> exchanges)
        {
            _disabledOrderbooks.AddOrUpdate(assetPairId, p => exchanges,
                (p, old) => old.Union(exchanges));
        }

        public void Enable(string assetPairId, string exchange)
        {
            _disabledOrderbooks.AddOrUpdate(assetPairId, p => ImmutableHashSet<string>.Empty,
                (p, old) => old.Remove(exchange));
        }
    }
}
