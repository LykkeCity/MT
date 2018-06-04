using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Attributes;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    /// <summary>                                                                                       
    /// Provides data about prices
    /// </summary>
    [Authorize]
    [Route("api/prices")]
    public class PricesController : Controller, IPricesApi
    {
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly IFxRateCacheService _fxRateCacheService;

        public PricesController(
            IQuoteCacheService quoteCacheService,
            IFxRateCacheService fxRateCacheService)
        {
            _quoteCacheService = quoteCacheService;
            _fxRateCacheService = fxRateCacheService;
        }

        /// <summary>
        /// Get current best prices
        /// </summary>
        /// <remarks>
        /// Post because the query string will be too long otherwise
        /// </remarks>
        [Route("best")]
        [HttpPost]
        [SkipMarginTradingEnabledCheck]
        public Task<Dictionary<string, BestPriceContract>> GetBestAsync(
            [FromBody] InitPricesBackendRequest request)
        {
            IEnumerable<KeyValuePair<string, InstrumentBidAskPair>> allQuotes = _quoteCacheService.GetAllQuotes();

            if (request.AssetIds != null && request.AssetIds.Any())
                allQuotes = allQuotes.Where(q => request.AssetIds.Contains(q.Key));

            return Task.FromResult(allQuotes.ToDictionary(q => q.Key, q => Convert(q.Value)));
        }

        /// <summary>
        /// Get current fx best prices
        /// </summary>
        /// <remarks>
        /// Post because the query string will be too long otherwise
        /// </remarks>
        [Route("bestFx")]
        [HttpPost]
        [SkipMarginTradingEnabledCheck]
        public Task<Dictionary<string, BestPriceContract>> GetBestFxAsync(
            [FromBody] InitPricesBackendRequest request)
        {
            IEnumerable<KeyValuePair<string, InstrumentBidAskPair>> allQuotes = _fxRateCacheService.GetAllQuotes();

            if (request.AssetIds != null && request.AssetIds.Any())
                allQuotes = allQuotes.Where(q => request.AssetIds.Contains(q.Key));

            return Task.FromResult(allQuotes.ToDictionary(q => q.Key, q => Convert(q.Value)));
        }
        
        private BestPriceContract Convert(InstrumentBidAskPair arg)
        {
            return new BestPriceContract
            {
                Ask = arg.Ask,
                Bid = arg.Bid,
                Id = arg.Instrument,
                Timestamp = arg.Date,
            };
        }
    }
}