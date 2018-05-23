using MarginTrading.Backend.Core.FakeExchangeConnector.Domain;
using MarginTrading.Backend.Core.FakeExchangeConnector.Domain.Trading;

namespace MarginTrading.Backend.Core.FakeExchangeConnector.Caches
{
    public interface IExchangeCache : IGenericDictionaryCache<Exchange>
    {
        Exchange UpdatePosition(string exchangeName, string instrument, TradeType tradeType, decimal volume);
    }
}
