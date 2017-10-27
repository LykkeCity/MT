using System;
using MarginTrading.Core;
using MarginTrading.Core.MatchedOrders;
using MarginTrading.Core.MatchingEngines;

namespace MarginTrading.Services.MatchingEngines
{
    public class RejectMatchingEngine : IMatchingEngineBase
    {
        public string Id => MatchingEngineConstants.Reject;
        
        public void MatchMarketOrderForOpen(Order order, Func<MatchedOrderCollection, bool> orderProcessed)
        {
            orderProcessed(new MatchedOrderCollection());
        }

        public void MatchMarketOrderForClose(Order order, Func<MatchedOrderCollection, bool> orderProcessed)
        {
            orderProcessed(new MatchedOrderCollection());
        }
    }
}