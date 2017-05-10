using System.Collections.Generic;

namespace MarginTrading.Core
{
    public interface IAggregatedOrderBook
    {
        List<OrderBookLevel> GetBuy(string instrumentId);
        List<OrderBookLevel> GetSell(string instrumentId);
        double? GetPriceFor(string orderInstrument, OrderDirection getCloseType);
    }
}