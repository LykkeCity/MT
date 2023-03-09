// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Quotes;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.Services;
using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Quotes
{
    /// <summary>
    /// Inspects the quotes returned from the cache.
    /// Logs a warning if the quote is not up to date (Is older than 5 seconds).
    /// </summary>
    internal sealed class QuoteCacheInspector : IQuoteCacheService
    {
        private readonly IQuoteCacheService _decoratee;
        private readonly IDateService _dateService;
        private readonly ILogger<QuoteCacheInspector> _logger;
        private readonly TimeSpan _quoteStalePeriod;
        private readonly ConcurrentDictionary<int, byte> _warnedQuotes = new ConcurrentDictionary<int, byte>();

        public QuoteCacheInspector(IQuoteCacheService decoratee,
            IDateService dateService,
            ILogger<QuoteCacheInspector> logger,
            MarginTradingSettings settings)
        {
            _decoratee = decoratee;
            _dateService = dateService;
            _logger = logger;
            _quoteStalePeriod = settings.Monitoring?.Quotes?.ConsiderQuoteStalePeriod ?? TimeSpan.FromSeconds(5);

            _logger.LogInformation("Quote Cache Inspector is activated with stale period {stalePeriod}",
                _quoteStalePeriod);
        }

        public InstrumentBidAskPair GetQuote(string instrument)
        {
            var quote = _decoratee.GetQuote(instrument);

            try
            {
                if (CanWarn(quote))
                    WarnOnStaleQuote(quote);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to inspect quote for {instrument}", instrument);
            }

            return quote;
        }

        public Dictionary<string, InstrumentBidAskPair> GetAllQuotes()
        {
            return _decoratee.GetAllQuotes();
        }

        public bool TryGetQuoteById(string instrument, out InstrumentBidAskPair result)
        {
            var success = _decoratee.TryGetQuoteById(instrument, out var quote);
            
            try
            {
                if (CanWarn(quote))
                    WarnOnStaleQuote(quote);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to inspect quote for {instrument}", instrument);
            }
            
            result = quote;
            return success;
        }

        public RemoveQuoteError RemoveQuote(string assetPairId)
        {
            return _decoratee.RemoveQuote(assetPairId);
        }

        public void WarnOnStaleQuote(InstrumentBidAskPair quote)
        {
            if (quote == null)
                return;

            _logger.LogWarning("Quote for {instrument} is stale. Quote date: {quoteDate}, now: {now}",
                quote.Instrument, quote.Date, _dateService.Now());
            
            _warnedQuotes.TryAdd(quote.GetStaleHash(), 0);
        }

        public bool CanWarn(InstrumentBidAskPair quote)
        {
            if (quote == null)
                return false;
            
            var current = _dateService.Now();
            if (IsQuoteStale(quote.Date, current, _quoteStalePeriod))
            {
                var hash = quote.GetStaleHash();
                return !_warnedQuotes.ContainsKey(hash);   
            }
            
            return false;
        }
        
        public static bool IsQuoteStale(DateTime quoteDateTime, DateTime now, TimeSpan stalePeriod)
        {
            return now.Subtract(quoteDateTime) > stalePeriod;
        }
    }
}