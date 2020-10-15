// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services
{
    [UsedImplicitly]
    public class FplService : IFplService
    {
        private readonly ICfdCalculatorService _cfdCalculatorService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly ITradingInstrumentsCacheService _tradingInstrumentsCache;
        private readonly MarginTradingSettings _marginTradingSettings;

        public FplService(
            ICfdCalculatorService cfdCalculatorService,
            IAssetPairsCache assetPairsCache,
            IAccountsCacheService accountsCacheService,
            ITradingInstrumentsCacheService tradingInstrumentsCache,
            MarginTradingSettings marginTradingSettings)
        {
            _cfdCalculatorService = cfdCalculatorService;
            _accountsCacheService = accountsCacheService;
            _tradingInstrumentsCache = tradingInstrumentsCache;
            _marginTradingSettings = marginTradingSettings;
        }

        public void UpdatePositionFpl(Position position)
        {
//            var handler = order.Status != OrderStatus.WaitingForExecution
//                ? UpdateOrderFplData
//                : (Action<IOrder, FplData>)UpdatePendingOrderMargin;
//
//            handler(order, fplData);

            UpdatePositionFplData(position, position.FplData);

        }

        public decimal GetInitMarginForOrder(Order order)
        {
            var accountAsset =
                _tradingInstrumentsCache.GetTradingInstrument(order.TradingConditionId, order.AssetPairId);
            var marginRate = _cfdCalculatorService.GetQuoteRateForBaseAsset(order.AccountAssetId, order.AssetPairId,
                order.LegalEntity, order.Direction == OrderDirection.Buy);
            var accountBaseAssetAccuracy = AssetsConstants.DefaultAssetAccuracy;

            return Math.Round(
                GetMargins(accountAsset, Math.Abs(order.Volume), marginRate).MarginInit,
                accountBaseAssetAccuracy);
        }

        public decimal CalculateOvernightMaintenanceMargin(Position position)
        {
            var fplData = new FplData {AccountBaseAssetAccuracy = position.FplData.AccountBaseAssetAccuracy};

            CalculateMargin(position, fplData, true);

            return fplData.MarginMaintenance;
        }

        //TODO: change the approach completely!
        private void UpdatePositionFplData(Position position, FplData fplData)
        {
            if (fplData.ActualHash == 0)
            {
                fplData.ActualHash = 1;
            }
            
            fplData.CalculatedHash = fplData.ActualHash;
            
            if (fplData.AccountBaseAssetAccuracy == default)
            {
                fplData.AccountBaseAssetAccuracy = AssetsConstants.DefaultAssetAccuracy;
            }
            
            fplData.RawFpl = (position.ClosePrice - position.OpenPrice) * position.CloseFxPrice * position.Volume;

            if (position.ClosePrice == 0)
                position.UpdateClosePrice(position.OpenPrice);
            
            CalculateMargin(position, fplData);
        }

        private void UpdatePendingOrderMargin(Position order, FplData fplData)
        {
            fplData.AccountBaseAssetAccuracy = AssetsConstants.DefaultAssetAccuracy;

            CalculateMargin(order, fplData);
            
            fplData.CalculatedHash = fplData.ActualHash;
            _accountsCacheService.Get(order.AccountId).CacheNeedsToBeUpdated();
        }

        private void CalculateMargin(Position position, FplData fplData, bool isWarnCheck = false)
        {
            var tradingInstrument =
                _tradingInstrumentsCache.GetTradingInstrument(position.TradingConditionId, position.AssetPairId);
            var volumeForCalculation = Math.Abs(position.Volume);

            fplData.MarginRate = position.ClosePrice * position.CloseFxPrice;

            var (marginInit, marginMaintenance) = GetMargins(tradingInstrument, volumeForCalculation,
                fplData.MarginRate, isWarnCheck);
            fplData.MarginInit = Math.Round(marginInit, fplData.AccountBaseAssetAccuracy);
            fplData.MarginMaintenance = Math.Round(marginMaintenance, fplData.AccountBaseAssetAccuracy);
            fplData.InitialMargin = Math.Round(position.OpenPrice * position.OpenFxPrice * volumeForCalculation /
                                                  tradingInstrument.LeverageInit, fplData.AccountBaseAssetAccuracy);
        }

        private (decimal MarginInit, decimal MarginMaintenance) GetMargins(ITradingInstrument tradingInstrument,
            decimal volumeForCalculation, decimal marginRate, bool isWarnCheck = false)
        {
            var (marginInit, marginMaintenance) = _tradingInstrumentsCache.GetMarginRates(tradingInstrument, isWarnCheck);

            return (volumeForCalculation * marginRate * marginInit, 
                volumeForCalculation * marginRate * marginMaintenance);
        }
    }
}
