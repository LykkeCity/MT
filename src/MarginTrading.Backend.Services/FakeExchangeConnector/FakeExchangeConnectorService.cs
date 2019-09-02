// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common.Chaos;
using Lykke.MarginTrading.OrderBookService.Contracts.Models;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.Service.ExchangeConnector.Client;
using Lykke.Service.ExchangeConnector.Client.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Services.FakeExchangeConnector
{
    public class FakeExchangeConnectorService : IExchangeConnectorService
    {
        private readonly IExternalOrderbookService _orderbookService;
        private readonly IChaosKitty _chaosKitty;
        private readonly MarginTradingSettings _settings;
        private readonly ILog _log;
        private readonly IDateService _dateService;
        private readonly ICqrsSender _cqrsSender;


        public FakeExchangeConnectorService(
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

        public void Dispose()
        {
        }

        public Task<HttpOperationResponse<IList<TradeBalanceModel>>> GetTradeBalanceWithHttpMessagesAsync(
            string exchangeName = null, Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<IList<string>>> GetSupportedExchangesWithHttpMessagesAsync(
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<ExchangeInformationModel>> GetExchangeInfoWithHttpMessagesAsync(
            string exchangeName, Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<IsAliveResponseModel>> IsAliveWithHttpMessagesAsync(
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<ExecutionReport>> GetOrderWithHttpMessagesAsync(string id,
            string exchangeName = null, string instrument = null,
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<ExecutionReport>> CancelOrderWithHttpMessagesAsync(string id,
            string exchangeName = null, Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<ExecutionReport>> CreateOrderWithHttpMessagesAsync(
            OrderModel orderModel = null, Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (orderModel == null || orderModel.Volume == 0)
            {
                var report = new HttpOperationResponse<ExecutionReport>
                {
                    Response = new HttpResponseMessage()
                    {
                        Content = new StringContent("Bad model"),
                        StatusCode = HttpStatusCode.BadRequest,
                    }
                };

                return Task.FromResult(report);
            }

            var result = new HttpOperationResponse<ExecutionReport>();
            
            try
            {
                _chaosKitty.Meow(nameof(FakeExchangeConnectorService));

                ExternalOrderBook orderbook;
                decimal? currentPrice;

                if (orderModel.Modality == TradeRequestModality.Liquidation)
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
                
                result.Body = new ExecutionReport(
                    type: orderModel.TradeType.ToType<TradeType>(),
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
                _log.WriteErrorAsync(nameof(FakeExchangeConnectorService), nameof(CreateOrderWithHttpMessagesAsync),
                    orderModel.ToJson(), ex);
                
                result.Body = new ExecutionReport(
                    type: orderModel.TradeType.ToType<TradeType>(),
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

        public Task<HttpOperationResponse<IList<PositionModel>>> GetOpenedPositionWithHttpMessagesAsync(
            string exchangeName = null, Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Uri BaseUri { get; set; }
        public JsonSerializerSettings SerializationSettings { get; set; }
        public JsonSerializerSettings DeserializationSettings { get; set; }
        public ServiceClientCredentials Credentials { get; set; }
    }
}