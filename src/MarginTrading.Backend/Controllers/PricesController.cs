// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Quotes;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.Mappers;
using MarginTrading.Contract.BackendContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    /// <inheritdoc cref="IPricesApi" />
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
        public Task<Dictionary<string, BestPriceContract>> GetBestAsync(
            [FromBody] InitPricesBackendRequest request)
        {
            IEnumerable<KeyValuePair<string, InstrumentBidAskPair>> allQuotes = _quoteCacheService.GetAllQuotes();

            if (request.AssetIds != null && request.AssetIds.Any())
                allQuotes = allQuotes.Where(q => request.AssetIds.Contains(q.Key));

            return Task.FromResult(allQuotes.ToDictionary(q => q.Key, q => q.Value.ConvertToContract()));
        }

        /// <summary>
        /// Get current fx best prices
        /// </summary>
        /// <remarks>
        /// Post because the query string will be too long otherwise
        /// </remarks>
        [Route("bestFx")]
        [HttpPost]
        public Task<Dictionary<string, BestPriceContract>> GetBestFxAsync(
            [FromBody] InitPricesBackendRequest request)
        {
            IEnumerable<KeyValuePair<string, InstrumentBidAskPair>> allQuotes = _fxRateCacheService.GetAllQuotes();

            if (request.AssetIds != null && request.AssetIds.Any())
                allQuotes = allQuotes.Where(q => request.AssetIds.Contains(q.Key));

            return Task.FromResult(allQuotes.ToDictionary(q => q.Key, q => q.Value.ConvertToContract()));
        }

        [HttpDelete]
        [Route("best/{assetPairId}")]
        public MtBackendResponse<bool> RemoveFromBestPriceCache(string assetPairId)
        {
            var result = _quoteCacheService.RemoveQuote(assetPairId);

            return result == RemoveQuoteErrorCode.None
                ? MtBackendResponse<bool>.Ok(true)
                : MtBackendResponse<bool>.Error(result.Message);
        }

        [HttpDelete]
        [Route("bestFx/{assetPairId}")]
        public MtBackendResponse<bool> RemoveFromBestFxPriceCache(string assetPairId)
        {
            var result = _fxRateCacheService.RemoveQuote(assetPairId);

            return result == RemoveQuoteErrorCode.None
                ? MtBackendResponse<bool>.Ok(true)
                : MtBackendResponse<bool>.Error(result.Message);
        }
    }
}