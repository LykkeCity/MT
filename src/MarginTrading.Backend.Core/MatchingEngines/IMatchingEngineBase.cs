using System;
using MarginTrading.Backend.Core.MatchedOrders;

namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IMatchingEngineBase
    {
        string Id { get; }
        
        void MatchMarketOrderForOpen(Order order, Func<MatchedOrderCollection, bool> orderProcessed);
        
        void MatchMarketOrderForClose(Order order, Func<MatchedOrderCollection, bool> orderProcessed);
    }
}
