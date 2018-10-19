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
        public string Id => MatchingEngineConstants.DefaultSpecialLiquidation;
        public MatchingEngineMode Mode => MatchingEngineMode.Stp;
        private readonly decimal _price;
        private readonly string _marketMakerId;
        private readonly string _externalOrderId;
        private readonly DateTime _externalExecutionTime;

        public SpecialLiquidationMatchingEngine(
            decimal price,
            string marketMakerId,
            string externalOrderId,
            DateTime externalExecutionTime)
        {
            _price = price;
            _marketMakerId = marketMakerId;
            _externalOrderId = externalOrderId;
            _externalExecutionTime = externalExecutionTime;
        }
        
        public Task<MatchedOrderCollection> MatchOrderAsync(Order order, bool shouldOpenNewPosition,
            OrderModality modality = OrderModality.Regular)
        {
            var col = new MatchedOrderCollection(new [] {new MatchedOrder
            {
                OrderId = _externalOrderId,
                MarketMakerId = _marketMakerId,
                Volume = Math.Abs(order.Volume),
                Price = _price,
                MatchedDate = _externalExecutionTime,
                IsExternal = true,
            }});
            return Task.FromResult(col);
        }

        public decimal? GetPriceForClose(string assetPairId, decimal volume, string externalProviderId)
        {
            return _price;
        }

        public OrderBook GetOrderBook(string instrument)
        {
            throw new System.NotImplementedException();
        }
    }
}