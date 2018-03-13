using System;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    public class StpMatchingEngine : IStpMatchingEngine
    {
        public string Id { get; }

        public MatchingEngineMode Mode => MatchingEngineMode.Stp;

        public StpMatchingEngine(string id)
        {
            Id = id;
        }
        
        public void MatchMarketOrderForOpen(Order order, Func<MatchedOrderCollection, bool> orderProcessed)
        {
            throw new NotImplementedException();
        }

        public void MatchMarketOrderForClose(Order order, Func<MatchedOrderCollection, bool> orderProcessed)
        {
            throw new NotImplementedException();
        }

        public OrderBook GetOrderBook(string instrument)
        {
            throw new NotImplementedException();
        }
    }
}