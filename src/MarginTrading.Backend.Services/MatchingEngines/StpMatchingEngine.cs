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
using MarginTrading.Backend.Core.Services;
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
        public string Id { get; }

        public MatchingEngineMode Mode => MatchingEngineMode.Stp;

        public StpMatchingEngine(string id, 
            IExternalOrderbookService externalOrderbookService,
            IExchangeConnectorService exchangeConnectorService,
            ILog log,
            IOperationsLogService operationsLogService,
            IDateService dateService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IAssetPairsCache assetPairsCache)
        {
            _externalOrderbookService = externalOrderbookService;
            _exchangeConnectorService = exchangeConnectorService;
            _log = log;
            _operationsLogService = operationsLogService;
            _dateService = dateService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _assetPairsCache = assetPairsCache;
            Id = id;
        }
        
        public async Task<MatchedOrderCollection> MatchOrderAsync(Order order, bool shouldOpenNewPosition)
        {
            var prices = _externalOrderbookService.GetPricesForExecution(order.AssetPairId, order.Volume, shouldOpenNewPosition);

            if (prices == null || !prices.Any())
            {
                return new MatchedOrderCollection();
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
                        tradeType: order.Direction.ToType<TradeType>(),
                        orderType: OrderType.Market,
                        timeInForce: TimeInForce.FillOrKill,
                        volume: (double) Math.Abs(order.Volume),
                        dateTime: _dateService.Now(),
                        exchangeName: sourcePrice.source,
                        instrument: externalAssetPair,
                        price: (double?)sourcePrice.price,
                        orderId: order.Id);

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
                    
                    _operationsLogService.AddLog("external order executed", order.AccountId, 
                        externalOrderModel.ToJson(), executionResult.ToJson());

                    return matchedOrders;
                }
                catch (Exception e)
                {
                    _log.WriteError($"{nameof(StpMatchingEngine)}:{nameof(MatchOrderAsync)}",
                        $"Internal order: {order.ToJson()}, External order model: {externalOrderModel.ToJson()}", e);
                }
            }

            return new MatchedOrderCollection();
        }

        public decimal? GetPriceForClose(Position position)
        {
            return _externalOrderbookService.GetPriceForPositionClose(position.AssetPairId, position.Volume,
                position.ExternalProviderId);
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