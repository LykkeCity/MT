// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common.Chaos;
using Lykke.MarginTrading.OrderBookService.Contracts.Models;
using MarginTrading.Backend.Contracts.ExchangeConnector;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.FakeExchangeConnector
{
    public class FakeExchangeConnectorClient : IExchangeConnectorClient
    {
        private readonly IExternalOrderbookService _orderbookService;
        private readonly IChaosKitty _chaosKitty;
        private readonly MarginTradingSettings _settings;
        private readonly ILog _log;
        private readonly IDateService _dateService;
        private readonly ICqrsSender _cqrsSender;
        
        public FakeExchangeConnectorClient(
            IExternalOrderbookService orderbookService,
            IChaosKitty chaosKitty,
            MarginTradingSettings settings,
            ILog log,
            IDateService dateService,
            ICqrsSender cqrsSender)
        {
            _chaosKitty = chaosKitty;
            _settings = settings;
            _log = log;
            _dateService = dateService;
            _cqrsSender = cqrsSender;
            _orderbookService = orderbookService;
        }
        
        public Task<ExecutionReport> ExecuteOrder(OrderModel orderModel, CancellationToken cancellationToken)
        {
            if (orderModel == null || orderModel.Volume == 0)
            {
                return Task.FromResult(new ExecutionReport
                {
                    Success = false,
                    ExecutionStatus = OrderExecutionStatus.Rejected,
                    FailureType = OrderStatusUpdateFailureType.ConnectorError,
                });
            }

            ExecutionReport result;
            
            try
            {
                _chaosKitty.Meow(nameof(FakeExchangeConnectorClient));

                ExternalOrderBook orderbook;
                decimal? currentPrice;

                if (orderModel.Modality == TradeRequestModality.Liquidation_CorporateAction
                    ||
                    orderModel.Modality == TradeRequestModality.Liquidation_MarginCall)
                {
                    if (orderModel.Price == null)
                    {
                        throw new InvalidOperationException("Order should have price specified in case of special liquidation");
                    }
                    
                    currentPrice = (decimal?) orderModel.Price;

                    orderbook = new ExternalOrderBook(MatchingEngineConstants.DefaultSpecialLiquidation,
                        orderModel.Instrument, _dateService.Now(), new
                            []
                            {
                                new VolumePrice
                                    {Price = currentPrice.Value, Volume = currentPrice.Value}
                            },
                        new
                            []
                            {
                                new VolumePrice
                                    {Price = currentPrice.Value, Volume = currentPrice.Value}
                            });
                }
                else
                {
                    orderbook = _orderbookService.GetOrderBook(orderModel.Instrument);

                    if (orderbook == null)
                    {
                        throw new InvalidOperationException("Orderbook was not found");
                    }

                    currentPrice = orderbook.GetMatchedPrice((decimal) orderModel.Volume,
                        orderModel.TradeType == TradeType.Buy ? OrderDirection.Buy : OrderDirection.Sell);
                }
                
                result = new ExecutionReport(
                    type: orderModel.TradeType,
                    time: DateTime.UtcNow,
                    price: (double) (currentPrice ?? throw new Exception("No price")),
                    volume: orderModel.Volume,
                    fee: 0,
                    success: true,
                    executionStatus: OrderExecutionStatus.Fill,
                    failureType: OrderStatusUpdateFailureType.None,
                    orderType: orderModel.OrderType,
                    execType: ExecType.Trade,
                    clientOrderId: Guid.NewGuid().ToString(),
                    exchangeOrderId: Guid.NewGuid().ToString(),
                    instrument: new Instrument(orderModel.Instrument, orderModel.ExchangeName));

                _cqrsSender.PublishEvent(new OrderExecutionOrderBookContract
                {
                    OrderId = orderModel.OrderId,
                    Volume = (decimal) orderModel.Volume,
                    OrderBook = new ExternalOrderBookContract
                    {
                        AssetPairId = orderbook.AssetPairId,
                        ExchangeName = orderbook.ExchangeName,
                        Timestamp = orderbook.Timestamp,
                        ReceiveTimestamp = _dateService.Now(),
                        Asks = orderbook.Asks.Select(a => new VolumePriceContract
                        {
                            Price = a.Price, Volume = a.Volume
                        }).ToList(),
                        Bids = orderbook.Bids.Select(a => new VolumePriceContract
                        {
                            Price = a.Price, Volume = a.Volume
                        }).ToList()
                    }
                }, _settings.Cqrs.ContextNames.Gavel);
            }
            catch (Exception ex)
            {
                _log.WriteErrorAsync(nameof(FakeExchangeConnectorClient), nameof(ExecuteOrder),
                    orderModel.ToJson(), ex);
                
                result = new ExecutionReport(
                    type: orderModel.TradeType,
                    time: DateTime.UtcNow,
                    price: 0,
                    volume: 0,
                    fee: 0,
                    success: false,
                    executionStatus: OrderExecutionStatus.Rejected,
                    failureType: OrderStatusUpdateFailureType.ExchangeError,
                    orderType: orderModel.OrderType,
                    execType: ExecType.Trade,
                    clientOrderId: null,
                    exchangeOrderId: null,
                    instrument: new Instrument(orderModel.Instrument, orderModel.ExchangeName));
            }

            return Task.FromResult(result);
        }
    }
}