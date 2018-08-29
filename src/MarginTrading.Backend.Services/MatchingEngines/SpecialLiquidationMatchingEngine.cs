using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    /// <summary>
    /// Matching Engine for Special Liquidation ONLY!
    /// Instance is created in CommandsHandler and passed to TradingEngine, no IoC registration here. 
    /// </summary>
    public class SpecialLiquidationMatchingEngine : ISpecialLiquidationMatchingEngine
    {
        private readonly IDateService _dateService;
        private readonly IIdentityGenerator _identityGenerator;
        
        public string Id => MatchingEngineConstants.DefaultSpecialLiquidation;
        public MatchingEngineMode Mode => MatchingEngineMode.MarketMaker;
        private readonly decimal _price;
        private readonly string _marketMaker;

        public SpecialLiquidationMatchingEngine(
            IDateService dateService, 
            IIdentityGenerator identityGenerator,
            decimal price,
            string marketMaker)
        {
            _dateService = dateService;
            _identityGenerator = identityGenerator;
            _price = price;
            _marketMaker = marketMaker;
        }
        
        public Task<MatchedOrderCollection> MatchOrderAsync(Order order, bool shouldOpenNewPosition)
        {
            var col = new MatchedOrderCollection(new [] {new MatchedOrder
            {
                OrderId = _identityGenerator.GenerateAlphanumericId(),
                MarketMakerId = _marketMaker,
                Volume = Math.Abs(order.Volume),
                Price = _price,
                MatchedDate = _dateService.Now(),
                IsExternal = true,
            }});
            return Task.FromResult(col);
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