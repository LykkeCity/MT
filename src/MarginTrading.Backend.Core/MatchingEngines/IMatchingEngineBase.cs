using System;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Orderbooks;

namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IMatchingEngineBase
    {
        string Id { get; }
        
        MatchingEngineMode Mode { get; }
        
        void MatchMarketOrderForOpen(Order order, Func<MatchedOrderCollection, bool> orderProcessed);
        
        void MatchMarketOrderForClose(Order order, Func<MatchedOrderCollection, bool> orderProcessed);
        
        OrderBook GetOrderBook(string instrument);
    }
}
