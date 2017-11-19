using System.Collections.Generic;
using MarginTrading.Backend.Core.Orderbooks;

namespace MarginTrading.Backend.Core
{
    public interface IAggregatedOrderBook
    {
        List<OrderBookLevel> GetBuy(string instrumentId);
        List<OrderBookLevel> GetSell(string instrumentId);
    }
}