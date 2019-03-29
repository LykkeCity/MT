using System;
using System.Collections.Generic;
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
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
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
        private readonly IExternalOrderbookService _externalOrderbookService;
        private readonly IExchangeConnectorService _exchangeConnectorService;
        private readonly ILog _log;
        private readonly IOperationsLogService _operationsLogService;
        private readonly IDateService _dateService;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly MarginTradingSettings _marginTradingSettings;
        public string Id { get; }

        public MatchingEngineMode Mode => MatchingEngineMode.Stp;

        public StpMatchingEngine(string id, 
            IExternalOrderbookService externalOrderbookService,
            IExchangeConnectorService exchangeConnectorService,
            ILog log,
            IOperationsLogService operationsLogService,
            IDateService dateService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IAssetPairsCache assetPairsCache,
            MarginTradingSettings marginTradingSettings)
        {
            _externalOrderbookService = externalOrderbookService;
            _exchangeConnectorService = exchangeConnectorService;
            _log = log;
            _operationsLogService = operationsLogService;
            _dateService = dateService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _assetPairsCache = assetPairsCache;
            _marginTradingSettings = marginTradingSettings;
            Id = id;
        }
        
        public async Task<MatchedOrderCollection> MatchOrderAsync(Order order, bool shouldOpenNewPosition,
            OrderModality modality = OrderModality.Regular)
        {
            var prices = _externalOrderbookService.GetOrderedPricesForExecution(order.AssetPairId, order.Volume, shouldOpenNewPosition);

            if (prices == null || !prices.Any())
            {
                return new MatchedOrderCollection();
            }

            var assetPair = _assetPairsCache.GetAssetPairByIdOrDefault(order.AssetPairId);
            var externalAssetPair = assetPair?.BasePairId ?? order.AssetPairId;

            foreach (var (source, price) in prices
                .Where(x => string.IsNullOrEmpty(order.ExternalProviderId) || x.source == order.ExternalProviderId))
            {
                var externalOrderModel = new OrderModel();

                var orderType = order.OrderType == Core.Orders.OrderType.Market 
                    ? OrderType.Market 
                    : OrderType.Limit;

                var targetPrice = order.OrderType == Core.Orders.OrderType.Market
                    ? (double?) price
                    : (double?) order.Price;
                
                try
                {
                    externalOrderModel = new OrderModel(
                        tradeType: order.Direction.ToType<TradeType>(),
                        orderType: orderType,
                        timeInForce: TimeInForce.FillOrKill,
                        volume: (double) Math.Abs(order.Volume),
                        dateTime: _dateService.Now(),
                        exchangeName: source,
                        instrument: externalAssetPair,
                        price: targetPrice,
                        orderId: order.Id,
                        modality: modality.ToType<TradeRequestModality>());

                    var executionResult = await _exchangeConnectorService.CreateOrderAsync(externalOrderModel);

                    if (!executionResult.Success)
                    {
                        throw new Exception(
                            $"External order was not executed. Status: {executionResult.ExecutionStatus}. Failure: {executionResult.FailureType}");
                    }

                    var executedPrice = Math.Abs(executionResult.Price) > 0
                        ? (decimal) executionResult.Price
                        : price.Value;

                    var matchedOrders = new MatchedOrderCollection
                    {
                        new MatchedOrder
                        {
                            MarketMakerId = source,
                            MatchedDate = _dateService.Now(),
                            OrderId = executionResult.ExchangeOrderId,
                            Price = CalculatePriceWithMarkups(assetPair, order.Direction, executedPrice),
                            Volume = (decimal) executionResult.Volume,
                            IsExternal = true
                        }
                    };

                    await _rabbitMqNotifyService.ExternalOrder(executionResult);
                    
                    _operationsLogService.AddLog("external order executed", order.AccountId, 
                        externalOrderModel.ToJson(), executionResult.ToJson());

                    return matchedOrders;
                }
                catch (Exception e)
                {
                    var connector =
                        _marginTradingSettings.ExchangeConnector == ExchangeConnectorType.FakeExchangeConnector
                            ? "Fake"
                            : _exchangeConnectorService.BaseUri.OriginalString;
                    
                    _log.WriteError(
                        $"{nameof(StpMatchingEngine)}:{nameof(MatchOrderAsync)}:{connector}",
                        $"Internal order: {order.ToJson()}, External order model: {externalOrderModel.ToJson()}", e);

                    if (orderType == OrderType.Limit)
                    {
                        return null;
                    }
                }
            }

            return new MatchedOrderCollection();
        }

        public (string externalProviderId, decimal? price) GetBestPriceForOpen(string assetPairId, decimal volume)
        {
            var prices = _externalOrderbookService.GetOrderedPricesForExecution(assetPairId, volume, true);

            return prices.FirstOrDefault();
        }

        public decimal? GetPriceForClose(string assetPairId, decimal volume, string externalProviderId)
        {
            return _externalOrderbookService.GetPriceForPositionClose(assetPairId, volume, externalProviderId);
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