using System.Collections.Immutable;

namespace MarginTrading.MarketMaker.Models
{
    public class Orderbook
    {
        public ImmutableArray<OrderbookPosition> Bids { get; }
        public ImmutableArray<OrderbookPosition> Asks { get; }

        public Orderbook(ImmutableArray<OrderbookPosition> bids, ImmutableArray<OrderbookPosition> asks)
        {
            Bids = bids;
            Asks = asks;
        }
    }
}