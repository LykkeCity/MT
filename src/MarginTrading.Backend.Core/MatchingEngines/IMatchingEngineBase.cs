using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Orderbooks;

namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IMatchingEngineBase
    {
        string Id { get; }
        
        MatchingEngineMode Mode { get; }
        
        Task MatchMarketOrderForOpen(Order order, Func<MatchedOrderCollection, Task<bool>> orderProcessed);
        
        void MatchMarketOrderForClose(Order order, Func<MatchedOrderCollection, bool> orderProcessed);
        
        decimal? GetPriceForClose(Order order);
        
        OrderBook GetOrderBook(string instrument);
    }
}
