using System.Collections.Immutable;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class OrderbooksService : IOrderbooksService
    {
        private readonly ReadWriteLockedDictionary<string, ImmutableDictionary<string, ExternalOrderbook>> _orderbooks =
            new ReadWriteLockedDictionary<string, ImmutableDictionary<string, ExternalOrderbook>>();

        public ImmutableDictionary<string, ExternalOrderbook> AddAndGetByAssetPair(ExternalOrderbook orderbook)
        {
            return _orderbooks.AddOrUpdate(orderbook.AssetPairId,
                k => ImmutableDictionary.Create<string, ExternalOrderbook>().Add(orderbook.ExchangeName, orderbook),
                (k, dict) => dict.SetItem(orderbook.ExchangeName, orderbook));
        }
    }
}
