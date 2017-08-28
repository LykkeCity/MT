using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Core;
using MarginTrading.Core.Exceptions;
using MarginTrading.Core.Messages;
using Rocks.Caching;

namespace MarginTrading.DataReader.Services.Implementation
{
    internal class QuotesSnapshotReaderService : IQuoteCacheService
    {
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly ICacheProvider _cacheProvider;
        private static readonly string BlobName = "Quotes";

        public QuotesSnapshotReaderService(IMarginTradingBlobRepository blobRepository, ICacheProvider cacheProvider)
        {
            _blobRepository = blobRepository;
            _cacheProvider = cacheProvider;
        }

        public InstrumentBidAskPair GetQuote(string instrument)
        {
            if (!GetQuotes().TryGetValue(instrument, out var quote))
                throw new QuoteNotFoundException(instrument, string.Format(MtMessages.QuoteNotFound, instrument));

            return quote;
        }

        public Dictionary<string, InstrumentBidAskPair> GetAllQuotes()
        {
            return GetQuotes().ToDictionary(x => x.Key, y => y.Value);
        }

        public bool TryGetQuoteById(string instrument, out InstrumentBidAskPair result)
        {
            if (!GetQuotes().TryGetValue(instrument, out var quote))
            {
                result = null;
                return false;
            }

            result = quote;
            return true;
        }

        private Dictionary<string, InstrumentBidAskPair> GetQuotes()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            return _cacheProvider.Get(nameof(QuotesSnapshotReaderService),
                () =>
                {
                    var readTask = _blobRepository.ReadAsync<Dictionary<string, InstrumentBidAskPair>>(
                            LykkeConstants.StateBlobContainer, BlobName);
                    var orderbookState = readTask.GetAwaiter().GetResult() ??
                                         new Dictionary<string, InstrumentBidAskPair>();
                    return new CachableResult<Dictionary<string, InstrumentBidAskPair>>(orderbookState,
                        CachingParameters.FromSeconds(10));
                });
        }
    }
}