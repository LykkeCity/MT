using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.OrderbookAggregator.Contracts.Messages;

namespace MarginTrading.Backend.Core.Services
{
    public interface IFxRateCacheService
    {
        InstrumentBidAskPair GetQuote(string instrument);
        Dictionary<string, InstrumentBidAskPair> GetAllQuotes();
        Task SetQuote(ExternalExchangeOrderbookMessage quote);
        void SetQuote(InstrumentBidAskPair bidAskPair);
    }
}