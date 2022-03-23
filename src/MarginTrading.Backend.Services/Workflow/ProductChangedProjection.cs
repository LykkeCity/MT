// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
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
            
            if(@event.ChangeType == ChangeType.Deletion)
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
                    foreach (var order in _orderReader.GetPending().Where(x => x.AssetPairId == @event.OldValue.ProductId))
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
                        new Exception($"{positions.Length} positions are opened for [{assetPairId}], first: [{positions.First().Id}]."));
                }
            }
        }

        private async Task HandleTradingDisabled(ProductContract product, string username)
        {
            if (product.IsTradingDisabled)
            {
                var allRfq = await RetrieveAllRfq(product.ProductId, canBePaused: true);

                foreach (var rfq in allRfq)
                {
                    await _rfqPauseService.AddAsync(rfq.Id, PauseSource.TradingDisabled,
                        new Initiator(username));
                }
            }
            else
            {
                var allRfq = await RetrieveAllRfq(product.ProductId, canBeResumed: true);

                foreach (var rfq in allRfq)
                {
                    await _rfqPauseService.ResumeAsync(rfq.Id, PauseCancellationSource.TradingDisabledChanged,
                        new Initiator(username));
                }
            }
        }

        private async Task<List<Rfq>> RetrieveAllRfq(string instrumentId, bool? canBePaused = null, bool? canBeResumed = null)
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
                    States = new RfqOperationState[]
                    {
                        RfqOperationState.Started,
                    }
                    
                }, skip, take);

                result.AddRange(resp.Contents);
                skip += take;

            } while (resp.Size > 0);

            return result
                .Where(x => !x.RequestedFromCorporateActions // ignore rfq from corporate actions
                            && x.PauseSummary?.PauseReason != PauseSource.Manual.ToString()) // ignore manually paused rfq
                .ToList();
        }
    }
}