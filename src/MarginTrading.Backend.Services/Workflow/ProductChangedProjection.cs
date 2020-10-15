// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
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
using MarginTrading.Backend.Core.Settings;
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
        private readonly MarginTradingSettings _mtSettings;
        private readonly ILog _log;

        public ProductChangedProjection(
            ITradingEngine tradingEngine,
            IAssetPairsCache assetPairsCache,
            IOrderReader orderReader,
            IScheduleSettingsCacheService scheduleSettingsCacheService,
            ITradingInstrumentsManager tradingInstrumentsManager,
            MarginTradingSettings mtSettings,
            ILog log)
        {
            _tradingEngine = tradingEngine;
            _assetPairsCache = assetPairsCache;
            _orderReader = orderReader;
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
            _tradingInstrumentsManager = tradingInstrumentsManager;
            _mtSettings = mtSettings;
            _log = log;
        }

        [UsedImplicitly]
        public async Task Handle(ProductChangedEvent @event)
        {
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
                }

                //We need to update cache if new product is added or if margin multiplier is updated
                if (@event.ChangeType == ChangeType.Creation || 
                    @event.OldValue.OvernightMarginMultiplier != @event.NewValue.OvernightMarginMultiplier)
                {
                    await _tradingInstrumentsManager.UpdateTradingInstrumentsCacheAsync();
                }

                var isAdded = _assetPairsCache.AddOrUpdate(AssetPair.CreateFromProduct(@event.NewValue,
                    _mtSettings.DefaultLegalEntitySettings.DefaultLegalEntity));

                if (@event.NewValue.TradingCurrency != AssetPairConstants.BaseCurrencyId)
                    _assetPairsCache.AddOrUpdate(AssetPair.CreateFromCurrency(@event.NewValue.TradingCurrency,
                        _mtSettings.DefaultLegalEntitySettings.DefaultLegalEntity));

                //only for product
                if (isAdded)
                    await _scheduleSettingsCacheService.UpdateScheduleSettingsAsync();
            }

            void CloseAllOrders()
            {
                try
                {
                    foreach (var order in _orderReader.GetPending().Where(x => x.AssetPairId == @event.OldValue.ProductId))
                    {
                        _tradingEngine.CancelPendingOrder(order.Id, null,@event.EventId, 
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
    }
}