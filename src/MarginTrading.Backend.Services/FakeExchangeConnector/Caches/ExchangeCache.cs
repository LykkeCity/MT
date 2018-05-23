using System.Linq;
using MarginTrading.Backend.Core.FakeExchangeConnector.Caches;
using MarginTrading.Backend.Core.FakeExchangeConnector.Domain;
using MarginTrading.Backend.Core.FakeExchangeConnector.Domain.Trading;

namespace MarginTrading.Backend.Services.FakeExchangeConnector.Caches
{
    public class ExchangeCache : GenericDictionaryCache<Exchange>, IExchangeCache
    {
        public Exchange UpdatePosition(string exchangeName, string instrument, TradeType tradeType, decimal volume)
        {
            lock (LockObj)
            {
                var exchange = _cache.TryGetValue(exchangeName, out var value) ? value : new Exchange("fake");

                var position = exchange.Positions.FirstOrDefault(x => x.Symbol == instrument) ?? new Position
                {
                    Symbol = instrument
                };
                
                exchange.Positions = exchange.Positions.Except(new [] {position}).ToList();
                
                position.PositionVolume += volume * (tradeType == TradeType.Buy ? 1 : -1);

                exchange.Positions = exchange.Positions.Concat(new[] {position}).ToList();
                
                return exchange;
            }
        }
    }
}
