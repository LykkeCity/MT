using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    public class RejectMatchingEngine : IMatchingEngineBase
    {
        public string Id => MatchingEngineConstants.Reject;

        public MatchingEngineMode Mode => MatchingEngineMode.MarketMaker;

        public Task MatchMarketOrderForOpenAsync(Order order, Func<MatchedOrderCollection, bool> orderProcessed)
        {
            orderProcessed(new MatchedOrderCollection());
            
            return Task.CompletedTask;
        }

        public Task MatchMarketOrderForCloseAsync(Order order, Func<MatchedOrderCollection, bool> orderProcessed)
        {
            orderProcessed(new MatchedOrderCollection());
            
            return Task.CompletedTask;
        }

        public decimal? GetPriceForClose(Order order)
        {
            return null;
        }

        public OrderBook GetOrderBook(string instrument)
        {
            return new OrderBook(instrument);
        }
    }
}