using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTradingTests.Services
{
    public class FakeMatchingEngine : IMatchingEngineBase
    {
        public string Id => MatchingEngineConstants.DefaultMm;
        public MatchingEngineMode Mode => MatchingEngineMode.Stp;
        private readonly decimal _openPrice;
        private readonly decimal _closePrice;
        private readonly string _marketMakerId;
        private readonly string _externalOrderId;
        private readonly DateTime _externalExecutionTime;

        public FakeMatchingEngine(
            decimal openPrice,
            decimal? closePrice = null,
            string marketMakerId = "fakeMarket",
            string externalOrderId = null,
            DateTime? externalExecutionTime = null)
        {
            _openPrice = openPrice;
            _closePrice = closePrice ?? openPrice;
            _marketMakerId = marketMakerId;
            _externalOrderId = externalOrderId;
            _externalExecutionTime = externalExecutionTime ?? DateTime.MinValue;
        }
        
        public Task<MatchedOrderCollection> MatchOrderAsync(Order order, bool shouldOpenNewPosition,
            OrderModality modality = OrderModality.Regular)
        {
            var col = new MatchedOrderCollection(new [] {new MatchedOrder
            {
                OrderId = _externalOrderId,
                MarketMakerId = _marketMakerId,
                Volume = Math.Abs(order.Volume),
                Price = string.IsNullOrEmpty(order.ExternalProviderId) ? _openPrice : _closePrice,
                MatchedDate = _externalExecutionTime,
                IsExternal = true,
            }});
            return Task.FromResult(col);
        }

        public (string externalProviderId, decimal? price) GetBestPriceForOpen(string assetPairId, decimal volume)
        {
            return (_marketMakerId, _openPrice);
        }

        public decimal? GetPriceForClose(string assetPairId, decimal volume, string externalProviderId)
        {
            return _closePrice;
        }

        public OrderBook GetOrderBook(string instrument)
        {
            throw new System.NotImplementedException();
        }
    }
}