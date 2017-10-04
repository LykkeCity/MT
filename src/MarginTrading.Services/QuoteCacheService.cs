using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Core;
using MarginTrading.Core.Exceptions;
using MarginTrading.Core.Messages;
using MarginTrading.Services.Events;

namespace MarginTrading.Services
{
    public class QuoteCacheService : TimerPeriod, IQuoteCacheService, IEventConsumer<BestPriceChangeEventArgs>
    {
        private readonly ILog _log;
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly IAccountAssetsCacheService _accountAssetsCache;
        private Dictionary<string, InstrumentBidAskPair> _quotes;
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();
        private static string BlobName = "Quotes";

        public QuoteCacheService(ILog log, IMarginTradingBlobRepository blobRepository, IAccountAssetsCacheService accountAssetsCache) : base(nameof(QuoteCacheService), 10000, log)
        {
            _log = log;
            _blobRepository = blobRepository;
            _accountAssetsCache = accountAssetsCache;
        }

        public InstrumentBidAskPair GetQuote(string instrument)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (!_quotes.TryGetValue(instrument, out var quote))
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
                if (!_quotes.TryGetValue(instrument, out var quote))
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

        public override void Start()
        {
            _quotes =
                _blobRepository
                    .Read<Dictionary<string, InstrumentBidAskPair>>(LykkeConstants.StateBlobContainer, BlobName)
                    ?.ToDictionary(d => d.Key, d => d.Value) ??
                new Dictionary<string, InstrumentBidAskPair>();

            base.Start();
        }

        public override Task Execute()
        {
            return DumpToRepository();
        }

        public override void Stop()
        {
            DumpToRepository().Wait();
            base.Stop();
        }

        private async Task DumpToRepository()
        {
            try
            {
                await _blobRepository.Write(LykkeConstants.StateBlobContainer, BlobName, GetAllQuotes());
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(QuoteCacheService), "Save quotes", "", ex);
            }
        }
    }
}
