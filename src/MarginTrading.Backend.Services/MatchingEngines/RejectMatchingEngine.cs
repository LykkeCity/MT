using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    public class RejectMatchingEngine : IMatchingEngineBase
    {
        public string Id => MatchingEngineConstants.Reject;

        public MatchingEngineMode Mode => MatchingEngineMode.MarketMaker;

        public Task<MatchedOrderCollection> MatchOrderAsync(Order order, bool shouldOpenNewPosition,
            OrderModality modality = OrderModality.Regular)
        {
            return Task.FromResult(new MatchedOrderCollection());
        }

        public Task MatchMarketOrderForCloseAsync(Position order, Func<MatchedOrderCollection, bool> orderProcessed)
        {
            orderProcessed(new MatchedOrderCollection());
            
            return Task.CompletedTask;
        }

        public decimal? GetPriceForClose(Position order)
        {
            return null;
        }

        public OrderBook GetOrderBook(string instrument)
        {
            return new OrderBook(instrument);
        }
    }
}