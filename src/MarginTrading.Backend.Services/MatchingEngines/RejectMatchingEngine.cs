using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    public class RejectMatchingEngine : IMatchingEngineBase
    {
        public string Id => MatchingEngineConstants.Reject;

        public MatchingEngineMode Mode => MatchingEngineMode.MarketMaker;

        public async Task MatchMarketOrderForOpen(Order order, Func<MatchedOrderCollection, Task<bool>> orderProcessed)
        {
            await orderProcessed(new MatchedOrderCollection());
        }

        public void MatchMarketOrderForClose(Order order, Func<MatchedOrderCollection, bool> orderProcessed)
        {
            orderProcessed(new MatchedOrderCollection());
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