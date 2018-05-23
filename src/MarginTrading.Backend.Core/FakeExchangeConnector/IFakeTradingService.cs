using System.Threading.Tasks;
using MarginTrading.Backend.Core.FakeExchangeConnector.Domain.Trading;

namespace MarginTrading.Backend.Core.FakeExchangeConnector
{
    public interface IFakeTradingService
    {
        Task<ExecutionReport> CreateOrder(string exchangeName, string instrument, TradeType tradeType,
            decimal price, decimal volume, bool isPublishToRabbit = true);

        bool? GetAcceptOrder(string exchangeName);
        bool? SetAcceptOrder(string exchangeName, bool acceptOrder);
        bool? GetPushEventToRabbit(string exchangeName);
        bool? SetPushEventToRabbit(string exchangeName, bool pushEventToRabbit);
    }
}
