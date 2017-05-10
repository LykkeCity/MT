using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MarginTrading.Core;
using MarginTrading.Core.Exceptions;
using MarginTrading.Core.Messages;
using MarginTrading.Services.Events;

namespace MarginTrading.Services
{
    public class QuoteCacheService : IQuoteCacheService,IEventConsumer<BestPriceChangeEventArgs>
    {
        private readonly Dictionary<string, InstrumentBidAskPair> _quotes = new Dictionary<string, InstrumentBidAskPair>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        ~QuoteCacheService()
        {
            _lockSlim?.Dispose();
        }

        public InstrumentBidAskPair GetQuote(string instrument)
        {
            _lockSlim.EnterReadLock();
            try
            {
                InstrumentBidAskPair quote;

                if (!_quotes.TryGetValue(instrument, out quote))
                    throw new QuoteNotFoundException(instrument, string.Format(MtMessages.QuoteNotFound, instrument));

                return quote;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public bool TryGetQuoteById(string instrument, out InstrumentBidAskPair result)
        {
            _lockSlim.EnterReadLock();
            try
            {
                InstrumentBidAskPair quote;

                if (!_quotes.TryGetValue(instrument, out quote))
                {
                    result = null;
                    return false;
                }

                result = quote;
                return true;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public Dictionary<string, InstrumentBidAskPair> GetAllQuotes()
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _quotes.ToDictionary(x => x.Key, y => y.Value);
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        int IEventConsumer.ConsumerRank => 100;

        void IEventConsumer<BestPriceChangeEventArgs>.ConsumeEvent(object sender, BestPriceChangeEventArgs ea)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                var bidAskPair = ea.BidAskPair;

                if (_quotes.ContainsKey(bidAskPair.Instrument))
                {
                    _quotes[bidAskPair.Instrument] = bidAskPair;
                }
                else
                {
                    _quotes.Add(bidAskPair.Instrument, bidAskPair);
                }
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }
    }
}
