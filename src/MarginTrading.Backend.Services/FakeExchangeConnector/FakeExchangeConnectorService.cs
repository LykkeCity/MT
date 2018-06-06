using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Service.ExchangeConnector.Client;
using Lykke.Service.ExchangeConnector.Client.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Services.FakeExchangeConnector
{
    public class FakeExchangeConnectorService : IExchangeConnectorService
    {
        private readonly IQuoteCacheService _quoteService;

        public FakeExchangeConnectorService(
            IQuoteCacheService quoteService)
        {
            _quoteService = quoteService;
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

        public async Task<HttpOperationResponse<ExecutionReport>> CreateOrderWithHttpMessagesAsync(OrderModel orderModel = null, Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (orderModel == null || orderModel.Volume == 0)
                return new HttpOperationResponse<ExecutionReport>
                {
                    Response = new HttpResponseMessage()
                    {
                        Content = new StringContent("Bad model"),
                        StatusCode = HttpStatusCode.BadRequest,
                    }
                };

            var quote = _quoteService.GetQuote(orderModel.Instrument);
            
            return new HttpOperationResponse<ExecutionReport>
            {
                Body = new ExecutionReport(
                    type: orderModel.TradeType.ToType<TradeType>(), 
                    time: DateTime.UtcNow, 
                    price: orderModel.Price ?? (double?)(orderModel.TradeType == TradeType.Buy ? quote?.Ask : quote?.Bid)
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
                    instrument: new Instrument(orderModel.Instrument, orderModel.ExchangeName))
            };
        }

        public async Task<HttpOperationResponse<IList<PositionModel>>> GetOpenedPositionWithHttpMessagesAsync(string exchangeName = null, Dictionary<string, List<string>> customHeaders = null,
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