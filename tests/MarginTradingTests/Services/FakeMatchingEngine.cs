// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;

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
        
        public ValueTask<MatchedOrderCollection> MatchOrderAsync(OrderFulfillmentPlan orderFulfillmentPlan,
            OrderModality modality = OrderModality.Regular)
        {
            return new ValueTask<MatchedOrderCollection>(
                new MatchedOrderCollection(
                    new[]
                    {
                        new MatchedOrder
                        {
                            OrderId = _externalOrderId,
                            MarketMakerId = _marketMakerId,
                            Volume = Math.Abs(orderFulfillmentPlan.UnfulfilledVolume),
                            Price = string.IsNullOrEmpty(orderFulfillmentPlan.Order.ExternalProviderId)
                                ? _openPrice
                                : _closePrice,
                            MatchedDate = _externalExecutionTime,
                            IsExternal = true,
                        }
                    }));
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
            throw new NotImplementedException();
        }
    }
}