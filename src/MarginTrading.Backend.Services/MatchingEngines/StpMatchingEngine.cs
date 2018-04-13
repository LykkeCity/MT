using System;
using System.Linq;
using Common;
using Common.Log;
using Lykke.Service.ExchangeConnector.Client;
using Lykke.Service.ExchangeConnector.Client.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Backend.Services.Stp;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    public class StpMatchingEngine : IStpMatchingEngine
    {
        private readonly ExternalOrderBooksList _externalOrderBooksList;
        private readonly IExchangeConnectorService _exchangeConnectorService;
        private readonly ILog _log;
        private readonly IDateService _dateService;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IAssetPairsCache _assetPairsCache;
        public string Id { get; }

        public MatchingEngineMode Mode => MatchingEngineMode.Stp;

        public StpMatchingEngine(string id, 
            ExternalOrderBooksList externalOrderBooksList,
            IExchangeConnectorService exchangeConnectorService,
            ILog log,
            IDateService dateService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IAssetPairsCache assetPairsCache)
        {
            _externalOrderBooksList = externalOrderBooksList;
            _exchangeConnectorService = exchangeConnectorService;
            _log = log;
            _dateService = dateService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _assetPairsCache = assetPairsCache;
            Id = id;
        }
        
        //TODO: remove orderProcessed function and make all validations before match
        public void MatchMarketOrderForOpen(Order order, Func<MatchedOrderCollection, bool> orderProcessed)
        {
            var prices = _externalOrderBooksList.GetPricesForOpen(order);

            if (prices == null)
            {
                orderProcessed(new MatchedOrderCollection());
                return;
            }
            
            prices = order.GetOrderType() == OrderDirection.Buy
                ? prices.OrderBy(tuple => tuple.price).ToList()
                : prices.OrderByDescending(tuple => tuple.price).ToList();
            
            var assetPair = _assetPairsCache.TryGetAssetPairById(order.Instrument);
            var externalAssetPair = assetPair?.BasePairId ?? order.Instrument;

            foreach (var sourcePrice in prices)
            {
                var externalOrderModel = new OrderModel();
                
                try
                {
                    externalOrderModel = new OrderModel(
                        order.GetOrderType().ToType<TradeType>(),
                        OrderType.Market,
                        TimeInForce.FillOrKill,
                        (double) Math.Abs(order.Volume),
                        _dateService.Now(),
                        sourcePrice.source,
                        externalAssetPair);

                    var executionResult = _exchangeConnectorService.CreateOrderAsync(externalOrderModel).GetAwaiter()
                        .GetResult();

                    var executedPrice = Math.Abs(executionResult.Price) > 0
                        ? (decimal) executionResult.Price
                        : sourcePrice.price.Value;

                    var matchedOrders = new MatchedOrderCollection
                    {
                        new MatchedOrder
                        {
                            ClientId = order.ClientId,
                            MarketMakerId = sourcePrice.source,
                            MatchedDate = _dateService.Now(),
                            OrderId = executionResult.ExchangeOrderId,
                            Price = CalculatePriceWithMarkups(assetPair, order.GetOrderType(), executedPrice),
                            Volume = (decimal) executionResult.Volume
                        }
                    };
                    
                    order.OpenExternalProviderId = sourcePrice.source;
                    order.OpenExternalOrderId = executionResult.ExchangeOrderId;

                    _rabbitMqNotifyService.ExternalOrder(executionResult).GetAwaiter().GetResult();

                    if (orderProcessed(matchedOrders))
                    {
                        return;
                    }
                    else
                    {
                        var cancelOrderModel = new OrderModel(
                            order.GetCloseType().ToType<TradeType>(),
                            OrderType.Market,
                            TimeInForce.FillOrKill,
                            (double) Math.Abs(order.Volume),
                            _dateService.Now(),
                            sourcePrice.source,
                            externalAssetPair);

                        var cancelOrderResult = _exchangeConnectorService.CreateOrderAsync(cancelOrderModel).GetAwaiter().GetResult();
                        
                        _rabbitMqNotifyService.ExternalOrder(cancelOrderResult).GetAwaiter().GetResult();
                    }

                    return;
                }
                catch (Exception e)
                {
                    _log.WriteErrorAsync(nameof(StpMatchingEngine), nameof(MatchMarketOrderForOpen),
                        $"Internal order: {order.ToJson()}, External order model: {externalOrderModel.ToJson()}", e);
                }
            }

            if (string.IsNullOrEmpty(order.OpenExternalProviderId) ||
                string.IsNullOrEmpty(order.OpenExternalOrderId))
            {
                order.Status = OrderStatus.Rejected;
                order.RejectReason = OrderRejectReason.NoLiquidity;
                order.RejectReasonText = "Error executing external order";
            }
            
        }

        //TODO: remove orderProcessed function and make all validations before match
        public void MatchMarketOrderForClose(Order order, Func<MatchedOrderCollection, bool> orderProcessed)
        {
            var closePrice = _externalOrderBooksList.GetPriceForClose(order);

            //TODO: rework!
            if (!closePrice.HasValue)
            {
                orderProcessed(new MatchedOrderCollection());
                return;
            }
            
            var closeLp = order.OpenExternalProviderId;
            var assetPair = _assetPairsCache.TryGetAssetPairById(order.Instrument);
            var externalAssetPair = assetPair?.BasePairId ?? order.Instrument;

            var externalOrderModel = new OrderModel();

            try
            {
                externalOrderModel = new OrderModel(
                    order.GetCloseType().ToType<TradeType>(),
                    OrderType.Market,
                    TimeInForce.FillOrKill,
                    (double) Math.Abs(order.Volume),
                    _dateService.Now(),
                    closeLp,
                    externalAssetPair);

                var executionResult = _exchangeConnectorService.CreateOrderAsync(externalOrderModel).GetAwaiter()
                    .GetResult();

                var executedPrice = Math.Abs(executionResult.Price) > 0
                    ? (decimal) executionResult.Price
                    : closePrice.Value;

                order.CloseExternalProviderId = closeLp;
                order.CloseExternalOrderId = executionResult.ExchangeOrderId;
                order.ClosePrice =
                    CalculatePriceWithMarkups(assetPair, order.GetCloseType(), executedPrice);

                _rabbitMqNotifyService.ExternalOrder(executionResult).GetAwaiter().GetResult();
                
                var matchedOrders = new MatchedOrderCollection
                {
                    new MatchedOrder
                    {
                        ClientId = order.ClientId,
                        MarketMakerId = closeLp,
                        MatchedDate = _dateService.Now(),
                        OrderId = executionResult.ExchangeOrderId,
                        Price = order.ClosePrice,
                        Volume = Math.Abs(order.Volume)
                    }
                };

                orderProcessed(matchedOrders);
            }
            catch (Exception e)
            {
                _log.WriteErrorAsync(nameof(StpMatchingEngine), nameof(MatchMarketOrderForClose),
                    $"Internal order: {order.ToJson()}, External order model: {externalOrderModel.ToJson()}", e);
            }

        }

        public decimal? GetPriceForClose(Order order)
        {
            return _externalOrderBooksList.GetPriceForClose(order);
        }

        //TODO: implement orderbook        
        public OrderBook GetOrderBook(string assetPairId)
        {
            //var orderbook = _externalOrderBooksList.GetOrderBook(instrument);
            
            return new OrderBook(assetPairId);
        }

        private decimal CalculatePriceWithMarkups(IAssetPair settings, OrderDirection direction, decimal sourcePrice)
        {
            if (settings == null)
                return sourcePrice;

            var markup = direction == OrderDirection.Buy ? settings.StpMultiplierMarkupAsk : settings.StpMultiplierMarkupBid;

            markup = markup != 0 ? markup : 1;

            return sourcePrice * markup;
        }
    }
}