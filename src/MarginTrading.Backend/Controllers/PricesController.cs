// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.ErrorCodes;
using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Quotes;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Mappers;
using MarginTrading.Contract.BackendContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    /// <inheritdoc cref="IPricesApi" />
    /// <summary>                                                                                       
    /// Prices management
    /// </summary>
    [Authorize]
    [Route("api/prices")]
    public class PricesController : Controller, IPricesApi
    {
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly IFxRateCacheService _fxRateCacheService;
        private readonly ITradingEngineSnapshotsRepository _tradingEngineSnapshotsRepository;
        private readonly ISnapshotService _snapshotService;
        private readonly ILifetimeScope _lifetimeScope;
        private readonly ILog _log;

        public PricesController(
            IQuoteCacheService quoteCacheService,
            IFxRateCacheService fxRateCacheService,
            ITradingEngineSnapshotsRepository tradingEngineSnapshotsRepository,
            ISnapshotService snapshotService,
            ILifetimeScope lifetimeScope,
            ILog log)
        {
            _quoteCacheService = quoteCacheService;
            _fxRateCacheService = fxRateCacheService;
            _tradingEngineSnapshotsRepository = tradingEngineSnapshotsRepository;
            _snapshotService = snapshotService;
            _lifetimeScope = lifetimeScope;
            _log = log;
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

        /// <inheritdoc />
        [HttpPost]
        [Route("missed")]
        public async Task<QuotesUploadErrorCode> UploadMissingQuotesAsync([FromBody] UploadMissingQuotesRequest request)
        {
            if (!DateTime.TryParse(request.TradingDay, out var tradingDay))
            {
                await _log.WriteWarningAsync(nameof(PricesController), 
                    nameof(UploadMissingQuotesAsync),
                    request.TradingDay, 
                    "Couldn't parse trading day");
                
                return QuotesUploadErrorCode.InvalidTradingDay;
            }

            using (var scope = _lifetimeScope.BeginLifetimeScope(ScopeConstants.SnapshotDraft))
            {
                var draftExists = await scope
                    .Resolve<IDraftSnapshotKeeper>()
                    .Init(tradingDay)
                    .ExistsAsync();

                using (var scope2 = _lifetimeScope.BeginLifetimeScope())
                {
                    var c = scope2.Resolve<IFinalSnapshotCalculator>();
                    await _log.WriteWarningAsync(nameof(PricesController), nameof(UploadMissingQuotesAsync), $"Calculator resolved? : {c != null}")
                }

                if (!draftExists)
                    return QuotesUploadErrorCode.NoDraft;

                try
                {
                    await _snapshotService.MakeTradingDataSnapshotFromDraft(
                        request.CorrelationId,
                        request.Underlyings,
                        request.Forex);
                }
                catch (InvalidOperationException e)
                {
                    await _log.WriteErrorAsync(nameof(PricesController), nameof(UploadMissingQuotesAsync), null, e);
                    return QuotesUploadErrorCode.AlreadyInProgress;
                }
                catch (ArgumentNullException e)
                {
                    await _log.WriteErrorAsync(nameof(PricesController), nameof(UploadMissingQuotesAsync), null, e);
                    return QuotesUploadErrorCode.EmptyQuotes;
                }
            }

            return QuotesUploadErrorCode.None;
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