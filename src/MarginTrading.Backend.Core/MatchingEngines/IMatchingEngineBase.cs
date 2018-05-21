using System;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IMatchingEngineBase
    {
        string Id { get; }
        
        MatchingEngineMode Mode { get; }
        
        void MatchMarketOrderForOpen(Order order, Func<MatchedOrderCollection, bool> orderProcessed);
        
        void MatchMarketOrderForClose(Order order, Func<MatchedOrderCollection, bool> orderProcessed);
        
        decimal? GetPriceForClose(Order order);
        
        OrderBook GetOrderBook(string instrument);
    }
}
