using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    /// <summary>
    /// Single process - single instance. Price is passed to the constructor.
    /// The impl must be private - living only in the same process. 
    /// </summary>
    public class SpecialLiquidationMatchingEngine : ISpecialLiquidationMatchingEngine
    {
        private readonly IDateService _dateService;
        
        public string Id => MatchingEngineConstants.DefaultSpecialLiquidation;
        public MatchingEngineMode Mode => MatchingEngineMode.MarketMaker;
        private readonly decimal _price;

        public SpecialLiquidationMatchingEngine(IDateService dateService, decimal price)
        {
            _dateService = dateService;
            _price = price;
        }
        
        public Task<MatchedOrderCollection> MatchOrderAsync(Order order, bool shouldOpenNewPosition)
        {
//            var col = new MatchedOrderCollection(new [] {new MatchedOrder
//            {
//                OrderId = ,
//                MarketMakerId = ,
//                LimitOrderLeftToMatch = ,
//                Volume = ,
//                Price = _price,
//                MatchedDate = _dateService.Now(),
//                IsExternal = ,
//            }});
//            return Task.FromResult(col);
        }

        public decimal? GetPriceForClose(Position order)
        {
            return _price;
        }

        public OrderBook GetOrderBook(string instrument)
        {
            throw new System.NotImplementedException();
        }
    }
}