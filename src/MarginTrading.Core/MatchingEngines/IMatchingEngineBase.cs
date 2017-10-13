using System;
using MarginTrading.Core.MatchedOrders;

namespace MarginTrading.Core.MatchingEngines
{
    public interface IMatchingEngineBase
    {
        string Id { get; }
        
        void MatchMarketOrderForOpen(Order order, Func<MatchedOrderCollection, bool> orderProcessed);
        
        void MatchMarketOrderForClose(Order order, Func<MatchedOrderCollection, bool> orderProcessed);
    }
}
