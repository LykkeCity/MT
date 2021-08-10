// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.ExchangeConnector;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;
using MarginTrading.Common.Settings;
using OrderType = MarginTrading.Backend.Contracts.ExchangeConnector.OrderType;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    public class StpMatchingEngine : IStpMatchingEngine
    {
        private readonly IExternalOrderbookService _externalOrderbookService;
        private readonly IExchangeConnectorClient _exchangeConnectorClient;
        private readonly ILog _log;
        private readonly IOperationsLogService _operationsLogService;
        private readonly IDateService _dateService;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly ExchangeConnectorServiceClient _exchangeConnectorServiceClient;
        private readonly IQuoteCacheService _quoteCacheService;
        public string Id { get; }

        public MatchingEngineMode Mode => MatchingEngineMode.Stp;

        public StpMatchingEngine(string id, 
            IExternalOrderbookService externalOrderbookService,
            IExchangeConnectorClient exchangeConnectorClient,
            ILog log,
            IOperationsLogService operationsLogService,
            IDateService dateService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IAssetPairsCache assetPairsCache,
            MarginTradingSettings marginTradingSettings,
            ExchangeConnectorServiceClient exchangeConnectorServiceClient,
            IQuoteCacheService quoteCacheService)
        {
            _externalOrderbookService = externalOrderbookService;
            _exchangeConnectorClient = exchangeConnectorClient;
            _log = log;
            _operationsLogService = operationsLogService;
            _dateService = dateService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _assetPairsCache = assetPairsCache;
            _marginTradingSettings = marginTradingSettings;
            _exchangeConnectorServiceClient = exchangeConnectorServiceClient;
            _quoteCacheService = quoteCacheService;
            Id = id;
        }
        
        public async Task<MatchedOrderCollection> MatchOrderAsync(Order order, bool shouldOpenNewPosition,
            OrderModality modality = OrderModality.Regular)
        {
            List<(string source, decimal? price)> prices = null;
            
            if (!string.IsNullOrEmpty(_marginTradingSettings.DefaultExternalExchangeId))
            {
                var quote = _quoteCacheService.GetQuote(order.AssetPairId);

                if (quote.GetVolumeForOrderDirection(order.Direction) >= Math.Abs(order.Volume))
                {
                    prices = new List<(string source, decimal? price)>
                    {
                        (_marginTradingSettings
                            .DefaultExternalExchangeId, quote.GetPriceForOrderDirection(order.Direction))
                    };
                }
            }
            
            if (prices == null)
            {
                prices = _externalOrderbookService.GetOrderedPricesForExecution(order.AssetPairId, order.Volume, shouldOpenNewPosition);

                if (prices == null || !prices.Any())
                {
                    return new MatchedOrderCollection();
                }
            }

            var assetPair = _assetPairsCache.GetAssetPairByIdOrDefault(order.AssetPairId);
            var externalAssetPair = assetPair?.BasePairId ?? order.AssetPairId;

            foreach (var (source, price) in prices
                .Where(x => string.IsNullOrEmpty(order.ExternalProviderId) || x.source == order.ExternalProviderId))
            {
                var externalOrderModel = new OrderModel();

                var orderType = order.OrderType == Core.Orders.OrderType.Limit
                                || order.OrderType == Core.Orders.OrderType.TakeProfit
                    ? Core.Orders.OrderType.Limit
                    : Core.Orders.OrderType.Market;

                var isCancellationTrade = order.AdditionalInfo.IsCancellationTrade(out var cancellationTradeExternalId);

                var targetPrice = order.OrderType != Core.Orders.OrderType.Market || isCancellationTrade
                    ? (double?) order.Price
                    : (double?) price;
                
                try
                {
                    externalOrderModel = new OrderModel(
                        tradeType: order.Direction.ToType<TradeType>(),
                        orderType: orderType.ToType<OrderType>(),
                        timeInForce: TimeInForce.FillOrKill,
                        volume: (double) Math.Abs(order.Volume),
                        dateTime: _dateService.Now(),
                        exchangeName: source,
                        instrument: externalAssetPair,
                        price: targetPrice,
                        orderId: order.Id,
                        modality: modality.ToType<TradeRequestModality>(),
                        isCancellationTrade: isCancellationTrade,
                        cancellationTradeExternalId: cancellationTradeExternalId);

                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(_marginTradingSettings.GavelTimeout);

                    var executionResult = await _exchangeConnectorClient.ExecuteOrder(externalOrderModel, cts.Token);

                    if (!executionResult.Success)
                    {
                        var ex = new Exception(
                            $"External order was not executed. Status: {executionResult.ExecutionStatus}. Failure: {executionResult.FailureType}");
                        LogOrderExecutionException(order, externalOrderModel, ex);
                    }
                    else
                    {
                        var executedPrice = Math.Abs(executionResult.Price) > 0
                            ? (decimal)executionResult.Price
                            : price.Value;

                        if (executedPrice.EqualsZero())
                        {
                            var ex = new Exception($"Have got execution price from Gavel equal to 0. Ignoring.");
                            LogOrderExecutionException(order, externalOrderModel, ex);
                        }
                        else
                        {
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
                    }
                }
                catch (Exception ex)
                {
                    LogOrderExecutionException(order, externalOrderModel, ex);
                    throw new OrderExecutionTechnicalException();
                }
            }

            return new MatchedOrderCollection();
        }

        private void LogOrderExecutionException(Order internalOrder, OrderModel externalOrderModel, Exception ex)
        {
            var connector = _marginTradingSettings.ExchangeConnector == ExchangeConnectorType.FakeExchangeConnector
                ? "Fake"
                : _exchangeConnectorServiceClient.ServiceUrl;

            _log.WriteError(
                $"{nameof(StpMatchingEngine)}:{nameof(MatchOrderAsync)}:{connector}",
                $"Internal order: {internalOrder.ToJson()}, External order model: {externalOrderModel.ToJson()}", ex);
        }

        public (string externalProviderId, decimal? price) GetBestPriceForOpen(string assetPairId, decimal volume)
        {
            var prices = _externalOrderbookService.GetOrderedPricesForExecution(assetPairId, volume, true);

            return prices?.FirstOrDefault() ?? default;
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