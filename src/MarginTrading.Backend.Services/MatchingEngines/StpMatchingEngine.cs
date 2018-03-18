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

            prices = order.GetOrderType() == OrderDirection.Buy
                ? prices.OrderBy(tuple => tuple.price).ToList()
                : prices.OrderByDescending(tuple => tuple.price).ToList();

            foreach (var price in prices)
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
                        price.source,
                        order.Instrument);

                    var executionResult = _exchangeConnectorService.CreateOrderAsync(externalOrderModel).GetAwaiter()
                        .GetResult();

                    var matchedOrders = new MatchedOrderCollection
                    {
                        new MatchedOrder
                        {
                            ClientId = order.ClientId,
                            MarketMakerId = price.source,
                            MatchedDate = _dateService.Now(),
                            OrderId = executionResult.ExchangeOrderId,
                            Price = CalculatePriceWithMarkups(order.Instrument, order.GetOrderType(), executionResult.Price),
                            Volume = (decimal) executionResult.Volume
                        }
                    };

                    if (orderProcessed(matchedOrders))
                    {
                        order.OpenExternalProviderId = price.source;
                        order.OpenExternalOrderId = executionResult.ExchangeOrderId;

                        _rabbitMqNotifyService.ExternalOrder(executionResult).GetAwaiter().GetResult();
                    }
                    else
                    {
                        var cancelOrderModel = new OrderModel(
                            order.GetCloseType().ToType<TradeType>(),
                            OrderType.Market,
                            TimeInForce.FillOrKill,
                            (double) Math.Abs(order.Volume),
                            _dateService.Now(),
                            price.source,
                            order.Instrument);

                        _exchangeConnectorService.CreateOrderAsync(cancelOrderModel).GetAwaiter().GetResult();
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
            var closeLp = order.OpenExternalOrderId;

            var matchedOrders = new MatchedOrderCollection();
            
            if (closePrice.HasValue)
            {
                matchedOrders.Add(new MatchedOrder
                {
                    ClientId = order.ClientId,
                    MarketMakerId = closeLp,
                    MatchedDate = _dateService.Now(),
                    OrderId = "",
                    Price = closePrice.Value,
                    Volume = Math.Abs(order.Volume)
                });
            }

            if (orderProcessed(matchedOrders))
            {
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
                        order.Instrument);
                
                    var executionResult = _exchangeConnectorService.CreateOrderAsync(externalOrderModel).GetAwaiter().GetResult();

                    order.CloseExternalProviderId = closeLp;
                    order.CloseExternalOrderId = executionResult.ExchangeOrderId;
                    order.ClosePrice =
                        CalculatePriceWithMarkups(order.Instrument, order.GetCloseType(), executionResult.Price);
                    
                    _rabbitMqNotifyService.ExternalOrder(executionResult).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    _log.WriteErrorAsync(nameof(StpMatchingEngine), nameof(MatchMarketOrderForClose), $"Internal order: {order.ToJson()}, External order model: {externalOrderModel.ToJson()}", e);
                }
            }
        }

        //TODO: implement orderbook        
        public OrderBook GetOrderBook(string assetPairId)
        {
            //var orderbook = _externalOrderBooksList.GetOrderBook(instrument);
            
            return new OrderBook(assetPairId);
        }

        private decimal CalculatePriceWithMarkups(string assetPairId, OrderDirection direction, double sourcePrice)
        {
            var settings = _assetPairsCache.GetAssetPairSettings(assetPairId);

            if (settings == null)
                return (decimal) sourcePrice;

            var markup = direction == OrderDirection.Buy ? settings.MultiplierMarkupAsk : settings.MultiplierMarkupBid;

            markup = markup != 0 ? markup : 1;

            return (decimal) sourcePrice * markup;
        }
    }
}