using System.Linq;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class BestPricesService : IBestPricesService
    {
        public BestPrices Calc(ExternalOrderbook orderbook)
        {
            return new BestPrices(
                orderbook.Bids.Max(b => b.Price),
                orderbook.Asks.Min(b => b.Price));
        }
    }
}
