// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Snow.Common;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.AssetService.Contracts.AssetPair;
using MarginTrading.AssetService.Contracts.Enums;
using MarginTrading.AssetService.Contracts.Products;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Contracts.Rfq;
using MarginTrading.Backend.Core.Quotes;
using MarginTrading.Backend.Core.Rfq;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services.Workflow
{
    /// <summary>
    /// Listens to <see cref="AssetPairChangedEvent"/>s and builds a projection inside of the
    /// <see cref="IAssetPairsCache"/>
    /// </summary>
    [UsedImplicitly]
    public class ProductChangedProjection
    {
        private readonly ITradingEngine _tradingEngine;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IOrderReader _orderReader;
        private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;
        private readonly ITradingInstrumentsManager _tradingInstrumentsManager;
        private readonly IRfqService _rfqService;
        private readonly IRfqPauseService _rfqPauseService;
        private readonly MarginTradingSettings _mtSettings;
        private readonly IQuoteCacheService _quoteCache;
        private readonly ILog _log;

        public ProductChangedProjection(
            ITradingEngine tradingEngine,
            IAssetPairsCache assetPairsCache,
            IOrderReader orderReader,
            IScheduleSettingsCacheService scheduleSettingsCacheService,
            ITradingInstrumentsManager tradingInstrumentsManager,
            IRfqService rfqService,
            IRfqPauseService rfqPauseService,
            MarginTradingSettings mtSettings,
            ILog log,
            IQuoteCacheService quoteCache)
        {
            _tradingEngine = tradingEngine;
            _assetPairsCache = assetPairsCache;
            _orderReader = orderReader;
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
            _tradingInstrumentsManager = tradingInstrumentsManager;
            _rfqService = rfqService;
            _rfqPauseService = rfqPauseService;
            _mtSettings = mtSettings;
            _log = log;
            _quoteCache = quoteCache;
        }

        [UsedImplicitly]
        public async Task Handle(ProductChangedEvent @event)
        {
            switch (@event.ChangeType)
            {
                case ChangeType.Creation:
                case ChangeType.Edition:
                    if (!@event.NewValue.IsStarted)
                    {
                        _log.WriteInfo(nameof(ProductChangedProjection),
                            nameof(Handle),
                            $"ProductChangedEvent received for productId: {@event.NewValue.ProductId}, but it was ignored because it has not been started yet.");
                        return;
                    }

                    break;
                case ChangeType.Deletion:
                    if (!@event.OldValue.IsStarted)
                    {
                        _log.WriteInfo(nameof(ProductChangedProjection),
                            nameof(Handle),
                            $"ProductChangedEvent received for productId: {@event.OldValue.ProductId}, but it was ignored because it has not been started yet.");
                        return;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (@event.ChangeType == ChangeType.Deletion)
            {
                CloseAllOrders();

                ValidatePositions(@event.OldValue.ProductId);

                _assetPairsCache.Remove(@event.OldValue.ProductId);
            }
            else
            {
                if (@event.NewValue.IsDiscontinued)
                {
                    CloseAllOrders();
                    RemoveQuoteFromCache();
                }

                await _tradingInstrumentsManager.UpdateTradingInstrumentsCacheAsync();

                var isAdded = _assetPairsCache.AddOrUpdate(AssetPair.CreateFromProduct(@event.NewValue,
                    _mtSettings.DefaultLegalEntitySettings.DefaultLegalEntity));

                if (@event.NewValue.TradingCurrency != AssetPairConstants.BaseCurrencyId)
                    _assetPairsCache.AddOrUpdate(AssetPair.CreateFromCurrency(@event.NewValue.TradingCurrency,
                        _mtSettings.DefaultLegalEntitySettings.DefaultLegalEntity));

                //only for product
                if (isAdded)
                    await _scheduleSettingsCacheService.UpdateScheduleSettingsAsync();

                if (@event.ChangeType == ChangeType.Edition &&
                    @event.OldValue.IsTradingDisabled != @event.NewValue.IsTradingDisabled)
                {
                    await HandleTradingDisabled(@event.NewValue, @event.Username);
                }
            }

            void RemoveQuoteFromCache()
            {
                var result = _quoteCache.RemoveQuote(@event.OldValue.ProductId);
                if (result != RemoveQuoteErrorCode.None)
                {
                    _log.WriteWarning(nameof(ProductChangedProjection), nameof(RemoveQuoteFromCache), result.Message);
                }
            }

            void CloseAllOrders()
            {
                try
                {
                    foreach (var order in _orderReader.GetPending()
                                 .Where(x => x.AssetPairId == @event.OldValue.ProductId))
                    {
                        _tradingEngine.CancelPendingOrder(order.Id, null,
                            null, OrderCancellationReason.InstrumentInvalidated);
                    }
                }
                catch (Exception exception)
                {
                    _log.WriteError(nameof(ProductChangedProjection), nameof(CloseAllOrders), exception);
                    throw;
                }
            }

            void ValidatePositions(string assetPairId)
            {
                var positions = _orderReader.GetPositions(assetPairId);
                if (positions.Any())
                {
                    _log.WriteFatalError(nameof(ProductChangedProjection), nameof(ValidatePositions),
                        new Exception(
                            $"{positions.Length} positions are opened for [{assetPairId}], first: [{positions.First().Id}]."));
                }
            }
        }

        private async Task HandleTradingDisabled(ProductContract product, string username)
        {
            if (product.IsTradingDisabled)
            {
                _log.WriteInfo(nameof(ProductChangedProjection), nameof(HandleTradingDisabled),
                    $"Trading disabled for product {product.ProductId}");
                var allRfq = await RetrieveAllRfq(product.ProductId, canBePaused: true);
                _log.WriteInfo(nameof(ProductChangedProjection), nameof(HandleTradingDisabled),
                    $"Found rfqs to pause: {allRfq.Select(x => x.Id).ToJson()}");

                foreach (var rfq in allRfq)
                {
                    _log.WriteInfo(nameof(ProductChangedProjection), nameof(HandleTradingDisabled),
                        $"Trying to pause rfq: {rfq.Id}");
                    await _rfqPauseService.AddAsync(rfq.Id,
                        PauseSource.TradingDisabled,
                        username);
                }
            }
            else
            {
                _log.WriteInfo(nameof(ProductChangedProjection), nameof(HandleTradingDisabled),
                    $"Trading enabled for product {product.ProductId}");
                var allRfq = await RetrieveAllRfq(product.ProductId, canBeResumed: true, canBeStopped: true);
                _log.WriteInfo(nameof(ProductChangedProjection), nameof(HandleTradingDisabled),
                    $"Found rfqs to resume or stop: {allRfq.Select(x => x.Id).ToJson()}");

                foreach (var rfq in allRfq)
                {
                    if (rfq.PauseSummary?.CanBeResumed ?? false)
                    {
                        _log.WriteInfo(nameof(ProductChangedProjection), nameof(HandleTradingDisabled),
                            $"Trying to resume rfq: {rfq.Id}");
                        await _rfqPauseService.ResumeAsync(rfq.Id,
                            PauseCancellationSource.TradingEnabled,
                            username);
                    }
                    else if (rfq.PauseSummary?.CanBeStopped ?? false)
                    {
                        _log.WriteInfo(nameof(ProductChangedProjection), nameof(HandleTradingDisabled),
                            $"Trying to stop pending pause for rfq: {rfq.Id}");
                        await _rfqPauseService.StopPendingAsync(rfq.Id,
                            PauseCancellationSource.TradingEnabled,
                            username);
                    }
                    else
                    {
                        _log.WriteWarning(nameof(ProductChangedProjection), nameof(HandleTradingDisabled),
                            $"Unexpected state for rfq: {rfq.Id}, {rfq.ToJson()}");
                    }
                }
            }
        }

        private async Task<List<Rfq>> RetrieveAllRfq(string instrumentId,
            bool? canBePaused = null,
            bool? canBeResumed = null,
            bool? canBeStopped = null)
        {
            var result = new List<Rfq>();
            PaginatedResponse<Rfq> resp;
            var skip = 0;
            var take = 20;
            do
            {
                resp = await _rfqService.GetAsync(new RfqFilter()
                {
                    InstrumentId = instrumentId,
                    CanBePaused = canBePaused,
                    CanBeResumed = canBeResumed,
                    CanBeStopped = canBeStopped,
                    States = new RfqOperationState[]
                    {
                        RfqOperationState.Started,
                        RfqOperationState.Initiated,
                        RfqOperationState.PriceRequested,
                    }
                }, skip, take);

                result.AddRange(resp.Contents);
                skip += take;
            } while (resp.Size > 0);

            return result
                .Where(x => !x.RequestedFromCorporateActions // ignore rfq from corporate actions
                            && x.PauseSummary?.PauseReason !=
                            PauseSource.Manual.ToString()) // ignore manually paused rfq
                .ToList();
        }
    }
}