using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Common.Chaos;
using Lykke.Service.ExchangeConnector.Client;
using Lykke.Service.ExchangeConnector.Client.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Extensions;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Services.FakeExchangeConnector
{
    public class FakeExchangeConnectorService : IExchangeConnectorService
    {
        private readonly IQuoteCacheService _quoteService;
        private readonly IChaosKitty _chaosKitty;

        public FakeExchangeConnectorService(
            IQuoteCacheService quoteService, 
            IChaosKitty chaosKitty)
        {
            _quoteService = quoteService;
            _chaosKitty = chaosKitty;
        }
        
        public void Dispose()
        {
        }

        public Task<HttpOperationResponse<IList<TradeBalanceModel>>> GetTradeBalanceWithHttpMessagesAsync(string exchangeName = null, Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<IList<string>>> GetSupportedExchangesWithHttpMessagesAsync(Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<ExchangeInformationModel>> GetExchangeInfoWithHttpMessagesAsync(string exchangeName, Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<IsAliveResponseModel>> IsAliveWithHttpMessagesAsync(Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<ExecutionReport>> GetOrderWithHttpMessagesAsync(string id, string exchangeName = null, string instrument = null,
            Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<ExecutionReport>> CancelOrderWithHttpMessagesAsync(string id, string exchangeName = null, Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<HttpOperationResponse<ExecutionReport>> CreateOrderWithHttpMessagesAsync(OrderModel orderModel = null, Dictionary<string, List<string>> customHeaders = null,
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
                
            var quote = _quoteService.GetQuote(orderModel.Instrument);  
            
            var result = new HttpOperationResponse<ExecutionReport>();
            
            try
            {
                _chaosKitty.Meow(nameof(FakeExchangeConnectorService));

                result.Body = new ExecutionReport(
                    type: orderModel.TradeType.ToType<TradeType>(),
                    time: DateTime.UtcNow,
                    price: orderModel.Price ??
                           (double?) (orderModel.TradeType == TradeType.Buy ? quote?.Ask : quote?.Bid)
                           ?? throw new Exception("No price"),
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
            }
            catch
            {
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

        public Task<HttpOperationResponse<IList<PositionModel>>> GetOpenedPositionWithHttpMessagesAsync(string exchangeName = null, Dictionary<string, List<string>> customHeaders = null,
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