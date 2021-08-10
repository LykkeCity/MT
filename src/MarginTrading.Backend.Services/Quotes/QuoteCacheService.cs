// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Stp;

namespace MarginTrading.Backend.Services.Quotes
{
    public class QuoteCacheService : TimerPeriod, IQuoteCacheService, IEventConsumer<BestPriceChangeEventArgs>
    {
        private readonly ILog _log;
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly IExternalOrderbookService _externalOrderbookService;
        
        private Dictionary<string, InstrumentBidAskPair> _cache = new Dictionary<string, InstrumentBidAskPair>();
        
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        private const string BlobName = "Quotes";

        public QuoteCacheService(ILog log, 
            IMarginTradingBlobRepository blobRepository,
            IExternalOrderbookService externalOrderbookService,
            MarginTradingSettings marginTradingSettings) 
            : base(nameof(QuoteCacheService), 
                marginTradingSettings.BlobPersistence.QuotesDumpPeriodMilliseconds, 
                log)
        {
            _log = log;
            _blobRepository = blobRepository;
            _externalOrderbookService = externalOrderbookService;
        }

        public override void Start()
        {
            _log.WriteInfo(nameof(QuoteCacheService), nameof(Start), "Quote cache init started.");
            
            var blobQuotes =
                _blobRepository
                    .Read<Dictionary<string, InstrumentBidAskPair>>(LykkeConstants.StateBlobContainer, BlobName)
                    ?.ToDictionary(d => d.Key, d => d.Value) ??
                new Dictionary<string, InstrumentBidAskPair>();
            _log.WriteInfo(nameof(QuoteCacheService), nameof(Start), 
                $"{blobQuotes.Count} quotes read from blob.");
            
            var orderBooks = _externalOrderbookService.GetOrderBooks();
            _log.WriteInfo(nameof(QuoteCacheService), nameof(Start), 
                $"{orderBooks.Count} order books read from {nameof(IExternalOrderbookService)}.");

            var result = new Dictionary<string, InstrumentBidAskPair>();
            foreach (var orderBook in orderBooks)
            {
                if (!blobQuotes.TryGetValue(orderBook.AssetPairId, out var quote)
                    || orderBook.Timestamp > quote.Date)
                {
                    result.Add(orderBook.AssetPairId, orderBook.GetBestPrice());
                }
            }

            foreach (var remainsToAdd in blobQuotes.Keys.Except(result.Keys))
            {
                var item = blobQuotes[remainsToAdd];
                result.Add(item.Instrument, item);
            }

            _cache = result;
            
            _log.WriteInfo(nameof(QuoteCacheService), nameof(Start), 
                $"Quote cache initialised with total {result.Count} items.");

            base.Start();
        }

        public override Task Execute()
        {
            return DumpToRepository();
        }

        public override void Stop()
        {
            if (Working)
            {
                DumpToRepository().Wait();    
            }
            
            base.Stop();
        }

        public InstrumentBidAskPair GetQuote(string instrument)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (!_cache.TryGetValue(instrument, out var quote))
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
                if (!_cache.TryGetValue(instrument, out var quote))
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
                return _cache.ToDictionary(x => x.Key, y => y.Value);
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public void RemoveQuote(string assetPairId)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                if (_cache.ContainsKey(assetPairId))
                    _cache.Remove(assetPairId);
                else
                    throw new QuoteNotFoundException(assetPairId, string.Format(MtMessages.QuoteNotFound, assetPairId));
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        int IEventConsumer.ConsumerRank => 100;

        void IEventConsumer<BestPriceChangeEventArgs>.ConsumeEvent(object sender, BestPriceChangeEventArgs ea)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                var bidAskPair = ea.BidAskPair;

                if (_cache.ContainsKey(bidAskPair.Instrument))
                {
                    _cache[bidAskPair.Instrument] = bidAskPair;
                }
                else
                {
                    _cache.Add(bidAskPair.Instrument, bidAskPair);
                }
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        private async Task DumpToRepository()
        {
            try
            {
                await _blobRepository.WriteAsync(LykkeConstants.StateBlobContainer, BlobName, GetAllQuotes());
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(QuoteCacheService), "Save quotes", "", ex);
            }
        }
    }
}
