using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services;
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
        private readonly OrdersCache _ordersCache;

        public PricesController(
            IQuoteCacheService quoteCacheService,
            IFxRateCacheService fxRateCacheService,
            OrdersCache ordersCache)
        {
            _quoteCacheService = quoteCacheService;
            _fxRateCacheService = fxRateCacheService;
            _ordersCache = ordersCache;
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
        public Task<Dictionary<string, BestPriceContract>> GetBestFxAsync(
            [FromBody] InitPricesBackendRequest request)
        {
            IEnumerable<KeyValuePair<string, InstrumentBidAskPair>> allQuotes = _fxRateCacheService.GetAllQuotes();

            if (request.AssetIds != null && request.AssetIds.Any())
                allQuotes = allQuotes.Where(q => request.AssetIds.Contains(q.Key));

            return Task.FromResult(allQuotes.ToDictionary(q => q.Key, q => Convert(q.Value)));
        }

        [HttpDelete]
        [Route("best/{assetPairId}")]
        public MtBackendResponse<bool> RemoveFromBestPriceCache(string assetPairId)
        {
            var positions = _ordersCache.Positions.GetPositionsByInstrument(assetPairId).ToList();
            if (positions.Any())
            {
                return MtBackendResponse<bool>.Error(
                    $"Cannot delete [{assetPairId}] best price because there are {positions.Count} opened positions.");
            }
            
            var orders = _ordersCache.Active.GetOrdersByInstrument(assetPairId).ToList();
            if (orders.Any())
            {
                return MtBackendResponse<bool>.Error(
                    $"Cannot delete [{assetPairId}] best price because there are {orders.Count} active orders.");
            }
            
            _quoteCacheService.RemoveQuote(assetPairId);
            
            return MtBackendResponse<bool>.Ok(true);
        }

        [HttpDelete]
        [Route("bestFx/{assetPairId}")]
        public MtBackendResponse<bool> RemoveFromBestFxPriceCache(string assetPairId)
        {
            var positions = _ordersCache.Positions.GetPositionsByFxInstrument(assetPairId).ToList();
            if (positions.Any())
            {
                return MtBackendResponse<bool>.Error(
                    $"Cannot delete [{assetPairId}] best FX price because there are {positions.Count} opened positions.");
            }
            
            var orders = _ordersCache.Active.GetOrdersByFxInstrument(assetPairId).ToList();
            if (orders.Any())
            {
                return MtBackendResponse<bool>.Error(
                    $"Cannot delete [{assetPairId}] best FX price because there are {orders.Count} active orders.");
            }
            
            _fxRateCacheService.RemoveQuote(assetPairId);
            
            return MtBackendResponse<bool>.Ok(true);
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