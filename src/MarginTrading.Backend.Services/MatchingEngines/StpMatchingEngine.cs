using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.ExchangeConnector.Client;
using Lykke.Service.ExchangeConnector.Client.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Backend.Services.Stp;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;
using OrderType = Lykke.Service.ExchangeConnector.Client.Models.OrderType;

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
        public async Task MatchMarketOrderForOpenAsync(Order order, Func<MatchedOrderCollection, bool> orderProcessed)
        {
            var prices = _externalOrderBooksList.GetPricesForExecution(order);

            if (prices == null)
            {
                orderProcessed(new MatchedOrderCollection());
                return;
            }
            
            prices = order.Direction == OrderDirection.Buy
                ? prices.OrderBy(tuple => tuple.price).ToList()
                : prices.OrderByDescending(tuple => tuple.price).ToList();
            
            var assetPair = _assetPairsCache.GetAssetPairByIdOrDefault(order.AssetPairId);
            var externalAssetPair = assetPair?.BasePairId ?? order.AssetPairId;

            foreach (var sourcePrice in prices)
            {
                var externalOrderModel = new OrderModel();
                
                try
                {
                    externalOrderModel = new OrderModel(
                        order.Direction.ToType<TradeType>(),
                        OrderType.Market,
                        TimeInForce.FillOrKill,
                        (double) Math.Abs(order.Volume),
                        _dateService.Now(),
                        sourcePrice.source,
                        externalAssetPair);

                    var executionResult = await _exchangeConnectorService.CreateOrderAsync(externalOrderModel);

                    var executedPrice = Math.Abs(executionResult.Price) > 0
                        ? (decimal) executionResult.Price
                        : sourcePrice.price.Value;

                    var matchedOrders = new MatchedOrderCollection
                    {
                        new MatchedOrder
                        {
                            MarketMakerId = sourcePrice.source,
                            MatchedDate = _dateService.Now(),
                            OrderId = executionResult.ExchangeOrderId,
                            Price = CalculatePriceWithMarkups(assetPair, order.Direction, executedPrice),
                            Volume = (decimal) executionResult.Volume,
                            IsExternal = true
                        }
                    };

                    await _rabbitMqNotifyService.ExternalOrder(executionResult);

                    if (orderProcessed(matchedOrders))
                    {
                        return;
                    }
                    else
                    {
                        var cancelOrderModel = new OrderModel(
                            order.Direction == OrderDirection.Buy ? TradeType.Sell : TradeType.Buy,
                            OrderType.Market,
                            TimeInForce.FillOrKill,
                            (double) Math.Abs(order.Volume),
                            _dateService.Now(),
                            sourcePrice.source,
                            externalAssetPair);

                        var cancelOrderResult = await _exchangeConnectorService.CreateOrderAsync(cancelOrderModel);
                        
                        await _rabbitMqNotifyService.ExternalOrder(cancelOrderResult);
                    }

                    return;
                }
                catch (Exception e)
                {
                    _log.WriteError($"{nameof(StpMatchingEngine)}:{nameof(MatchMarketOrderForOpenAsync)}",
                        $"Internal order: {order.ToJson()}, External order model: {externalOrderModel.ToJson()}", e);
                }
            }

            if (string.IsNullOrEmpty(order.ExternalProviderId) ||
                string.IsNullOrEmpty(order.ExternalOrderId))
            {
                order.Reject(OrderRejectReason.NoLiquidity, "Error executing external order", "", _dateService.Now());
            }
            
        }

        //TODO: remove orderProcessed function and make all validations before match
        public async Task MatchMarketOrderForCloseAsync(Order order, Func<MatchedOrderCollection, bool> orderProcessed)
        {
            //var closePrice = _externalOrderBooksList.GetPriceForClose(order);

//            //TODO: rework!
//            if (!closePrice.HasValue)
//            {
//                orderProcessed(new MatchedOrderCollection());
//                return;
//            }
//            
//            var closeLp = order.ExternalProviderId;
//            var assetPair = _assetPairsCache.GetAssetPairByIdOrDefault(order.AssetPairId);
//            var externalAssetPair = assetPair?.BasePairId ?? order.AssetPairId;
//
//            var externalOrderModel = new OrderModel();

//            try
//            {
//                externalOrderModel = new OrderModel(
//                    order.GetCloseType().ToType<TradeType>(),
//                    OrderType.Market,
//                    TimeInForce.FillOrKill,
//                    (double) Math.Abs(order.Volume),
//                    _dateService.Now(),
//                    closeLp,
//                    externalAssetPair);
//
//                var executionResult = await _exchangeConnectorService.CreateOrderAsync(externalOrderModel);
//
//                var executedPrice = Math.Abs(executionResult.Price) > 0
//                    ? (decimal) executionResult.Price
//                    : closePrice.Value;
//
//                order.CloseExternalProviderId = closeLp;
//                order.CloseExternalOrderId = executionResult.ExchangeOrderId;
//                order.ClosePrice =
//                    CalculatePriceWithMarkups(assetPair, order.GetCloseType(), executedPrice);
//
//                await _rabbitMqNotifyService.ExternalOrder(executionResult);
//                
//                var matchedOrders = new MatchedOrderCollection
//                {
//                    new MatchedOrder
//                    {
//                        MarketMakerId = closeLp,
//                        MatchedDate = _dateService.Now(),
//                        OrderId = executionResult.ExchangeOrderId,
//                        Price = order.ClosePrice,
//                        Volume = Math.Abs(order.Volume)
//                    }
//                };
//
//                orderProcessed(matchedOrders);
//            }
//            catch (Exception e)
//            {
//                _log.WriteError($"{nameof(StpMatchingEngine)}:{nameof(MatchMarketOrderForCloseAsync)}",
//                    $"Internal order: {order.ToJson()}, External order model: {externalOrderModel.ToJson()}", e);
//            }

        }

        public decimal? GetPriceForClose(Position order)
        {
            return _externalOrderBooksList.GetPriceForPositionClose(order);
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