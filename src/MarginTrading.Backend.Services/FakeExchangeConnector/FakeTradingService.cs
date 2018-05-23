using System;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.FakeExchangeConnector;
using MarginTrading.Backend.Core.FakeExchangeConnector.Caches;
using MarginTrading.Backend.Core.FakeExchangeConnector.Domain.Trading;

namespace MarginTrading.Backend.Services.FakeExchangeConnector
{
    public class FakeTradingService : IFakeTradingService
    {
        private readonly IExchangeCache _exchangeCache;
        public FakeTradingService(IExchangeCache exchangeCache)
        {
            _exchangeCache = exchangeCache;
        }
        
        public async Task<ExecutionReport> CreateOrder(string exchangeName, string instrument, TradeType tradeType, 
            decimal price, decimal volume, bool isPublishToRabbit = true)
        {
            var executionReport = new ExecutionReport
            {
                Instrument = new Instrument(exchangeName, instrument),
                Time = DateTime.UtcNow,
                Price = price,
                Volume = volume,
                Type = tradeType,
                Fee = 0,
                ExchangeOrderId = Guid.NewGuid().ToString(),
                ExecutionStatus = OrderExecutionStatus.Fill,
                ExecType = ExecType.Trade,
                OrderType = OrderType.Market,
                Success = true
            };
            
            var exchange = _exchangeCache.UpdatePosition(exchangeName, instrument, tradeType, volume);
            
            return executionReport;
        }

        public bool? GetAcceptOrder(string exchangeName)
        {
            return _exchangeCache.Get(exchangeName)?.AcceptOrder;
        }

        public bool? SetAcceptOrder(string exchangeName, bool acceptOrder)
        {
            var exchange = _exchangeCache.Get(exchangeName);

            if (exchange == null)
                return null;

            exchange.AcceptOrder = acceptOrder;
            
            _exchangeCache.Set(exchange);

            return acceptOrder;
        }

        public bool? GetPushEventToRabbit(string exchangeName)
        {
            return _exchangeCache.Get(exchangeName)?.PushEventToRabbit;
        }

        public bool? SetPushEventToRabbit(string exchangeName, bool pushEventToRabbit)
        {
            var exchange = _exchangeCache.Get(exchangeName);

            if (exchange == null)
                return null;

            exchange.PushEventToRabbit = pushEventToRabbit;
            
            _exchangeCache.Set(exchange);

            return pushEventToRabbit;
        }
    }
}
