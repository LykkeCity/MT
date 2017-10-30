using System.Collections.Generic;

namespace MarginTrading.Backend.Core
{
    public interface IAggregatedOrderBook
    {
        List<OrderBookLevel> GetBuy(string instrumentId);
        List<OrderBookLevel> GetSell(string instrumentId);
    }
}