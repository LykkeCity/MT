using System.Collections.Generic;

namespace MarginTrading.Backend.Core
{
    public interface IQuoteCacheService
    {
        InstrumentBidAskPair GetQuote(string instrument);
        Dictionary<string, InstrumentBidAskPair> GetAllQuotes();
        bool TryGetQuoteById(string instrument, out InstrumentBidAskPair result); 
        void RemoveQuote(string assetPair);
    }
}
