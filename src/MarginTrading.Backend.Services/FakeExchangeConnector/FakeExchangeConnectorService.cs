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
using MarginTrading.Backend.Core.FakeExchangeConnector;
using MarginTrading.Backend.Core.FakeExchangeConnector.Caches;
using MarginTrading.Common.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Services.FakeExchangeConnector
{
    public class FakeExchangeConnectorService : IExchangeConnectorService
    {
        private readonly IExchangeCache _exchangeCache;
        private readonly IFakeTradingService _tradingService;
        private readonly IQuoteCacheService _quoteService;

        public FakeExchangeConnectorService(IExchangeCache exchangeCache,
            IFakeTradingService tradingService,
            IQuoteCacheService quoteService)
        {
            _exchangeCache = exchangeCache;
            _tradingService = tradingService;
            _quoteService = quoteService;
        }
        
        public void Dispose()
        {
        }

        public Task<HttpOperationResponse<IList<TradeBalanceModel>>> GetTradeBalanceWithHttpMessagesAsync(string exchangeName = null, Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var result = _exchangeCache.Get(exchangeName)?.Accounts.Select(x => new TradeBalanceModel
            {
                AccountCurrency = x.Asset,
                Totalbalance = (double) x.Balance,
                MarginUsed = 0,
                MaringAvailable = 100000,
                UnrealisedPnL = 0
            });
            return Task.FromResult(new HttpOperationResponse<IList<TradeBalanceModel>>
            {
                Body = result?.ToList(),
            });
        }

        public Task<HttpOperationResponse<IList<string>>> GetSupportedExchangesWithHttpMessagesAsync(Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var result = _exchangeCache.GetAll().Select(x => x.Name);
            return Task.FromResult(new HttpOperationResponse<IList<string>>
            {
                Body = result.ToList(),
            });
        }

        public Task<HttpOperationResponse<ExchangeInformationModel>> GetExchangeInfoWithHttpMessagesAsync(string exchangeName, Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var result = _exchangeCache.Get(exchangeName);

            if (result == null)
                return Task.FromResult(new HttpOperationResponse<ExchangeInformationModel>
                {
                    Response = new HttpResponseMessage()
                    {
                        Content = new StringContent($"Invalid {nameof(exchangeName)}"),
                        StatusCode = HttpStatusCode.BadRequest,
                    }
                });

            return Task.FromResult(new HttpOperationResponse<ExchangeInformationModel>
            {
                Body = new ExchangeInformationModel
                {
                    Instruments = result.Instruments.Select(x => new Instrument(x.Name, x.Exchange, x.Base, x.Quote)).ToList(),
                    Name = result.Name,
                    State = ExchangeState.Connected,
                    StreamingSupport = new StreamingSupport(result.StreamingSupport.OrderBooks, result.StreamingSupport.Orders)
                }
            });
        }

        public Task<HttpOperationResponse<IsAliveResponseModel>> IsAliveWithHttpMessagesAsync(Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public async Task<HttpOperationResponse<ExecutionReport>> GetOrderWithHttpMessagesAsync(string id, string exchangeName = null, string instrument = null,
            Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public async Task<HttpOperationResponse<ExecutionReport>> CancelOrderWithHttpMessagesAsync(string id, string exchangeName = null, Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public async Task<HttpOperationResponse<ExecutionReport>> CreateOrderWithHttpMessagesAsync(OrderModel orderModel = null, Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            if (orderModel.Volume == 0)
                return new HttpOperationResponse<ExecutionReport>
                {
                    Response = new HttpResponseMessage()
                    {
                        Content = new StringContent("Bad model"),
                        StatusCode = HttpStatusCode.BadRequest,
                    }
                };

            var quote = _quoteService.GetQuote(orderModel.Instrument);
            
            var result = await _tradingService.CreateOrder(
                orderModel.ExchangeName, 
                orderModel.Instrument,
                orderModel.TradeType.ToType<MarginTrading.Backend.Core.FakeExchangeConnector.Domain.Trading.TradeType>(), 
                (decimal?) orderModel.Price ?? (orderModel.TradeType == TradeType.Buy ? quote?.Ask : quote?.Bid) 
                    ?? throw new Exception("No price"), 
                (decimal) orderModel.Volume);

            return new HttpOperationResponse<ExecutionReport>
            {
                Body = new ExecutionReport(result.Type.ToType<TradeType>(), 
                    result.Time, (double) result.Price, (double) result.Volume, (double) result.Fee, 
                    result.Success, result.ExecutionStatus.ToType<OrderExecutionStatus>(), 
                    result.FailureType.ToType<OrderStatusUpdateFailureType>(), 
                    result.OrderType.ToType<OrderType>(), 
                    result.ExecType.ToType<ExecType>(),
                    result.ClientOrderId, result.ExchangeOrderId, 
                    new Instrument(result.Instrument.Name, result.Instrument.Exchange, result.Instrument.Base, result.Instrument.Quote), 
                    result.FeeCurrency, result.Message)
            };
        }

        public async Task<HttpOperationResponse<IList<PositionModel>>> GetOpenedPositionWithHttpMessagesAsync(string exchangeName = null, Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var result = _exchangeCache.Get(exchangeName)?.Positions.Select(x => new PositionModel
            {
                Symbol = x.Symbol,
                PositionVolume = (double) x.PositionVolume
            });
            return new HttpOperationResponse<IList<PositionModel>>
            {
                Body = result?.ToList()
            };
        }

        public Uri BaseUri { get; set; }
        public JsonSerializerSettings SerializationSettings { get; set; }
        public JsonSerializerSettings DeserializationSettings { get; set; }
        public ServiceClientCredentials Credentials { get; set; }
    }
}