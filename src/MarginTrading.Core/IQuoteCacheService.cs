using System.Collections.Generic;

namespace MarginTrading.Core
{
    public interface IQuoteCacheService
    {
        InstrumentBidAskPair GetQuote(string instrument);
        Dictionary<string, InstrumentBidAskPair> GetAllQuotes();
        bool TryGetQuoteById(string instrument, out InstrumentBidAskPair result);
        void StopApplication();
    }
}
