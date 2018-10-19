using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services
{
    [UsedImplicitly]
    public class FplService : IFplService
    {
        private readonly ICfdCalculatorService _cfdCalculatorService;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly ITradingInstrumentsCacheService _tradingInstrumentsCache;
        private readonly IAssetsCache _assetsCache;

        public FplService(
            ICfdCalculatorService cfdCalculatorService,
            IAssetPairsCache assetPairsCache,
            IAccountsCacheService accountsCacheService,
            ITradingInstrumentsCacheService tradingInstrumentsCache,
            IAssetsCache assetsCache)
        {
            _cfdCalculatorService = cfdCalculatorService;
            _assetPairsCache = assetPairsCache;
            _accountsCacheService = accountsCacheService;
            _tradingInstrumentsCache = tradingInstrumentsCache;
            _assetsCache = assetsCache;
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
            var accountBaseAssetAccuracy = _assetsCache.GetAssetAccuracy(order.AccountAssetId);

            return Math.Round(Math.Abs(order.Volume) * marginRate / accountAsset.LeverageInit,
                accountBaseAssetAccuracy);

        }

        private void UpdatePositionFplData(Position position, FplData fplData)
        {
            fplData.AccountBaseAssetAccuracy = _assetsCache.GetAssetAccuracy(position.AccountAssetId);
            fplData.FplRate = _cfdCalculatorService.GetQuoteRateForQuoteAsset(position.AccountAssetId,
                position.AssetPairId, position.LegalEntity,
                position.Volume * (position.ClosePrice - position.OpenPrice) > 0);

            var fpl = (position.ClosePrice - position.OpenPrice) * fplData.FplRate * position.Volume;

            fplData.Fpl = Math.Round(fpl, fplData.AccountBaseAssetAccuracy);

            if (position.ClosePrice == 0)
                position.UpdateClosePrice(position.OpenPrice);
            
            fplData.OpenPrice = position.OpenPrice;
            fplData.ClosePrice = position.ClosePrice;
            
            CalculateMargin(position, fplData);
            
            fplData.SwapsSnapshot = position.GetSwaps();

            if (fplData.ActualHash == 0)
            {
                fplData.ActualHash = 1;
            }
            fplData.CalculatedHash = fplData.ActualHash;
            
            fplData.TotalFplSnapshot = position.GetTotalFpl(fplData.SwapsSnapshot);

            _accountsCacheService.Get(position.AccountId).CacheNeedsToBeUpdated();
        }

        private void UpdatePendingOrderMargin(Position order, FplData fplData)
        {
            fplData.AccountBaseAssetAccuracy = _assetsCache.GetAssetAccuracy(order.AccountAssetId);
            
            CalculateMargin(order, fplData);
            
            fplData.CalculatedHash = fplData.ActualHash;
            _accountsCacheService.Get(order.AccountId).CacheNeedsToBeUpdated();
        }

        private void CalculateMargin(Position position, FplData fplData)
        {
            var tradingInstrument =
                _tradingInstrumentsCache.GetTradingInstrument(position.TradingConditionId, position.AssetPairId);

            fplData.MarginRate = _cfdCalculatorService.GetQuoteRateForBaseAsset(position.AccountAssetId, position.AssetPairId, 
                position.LegalEntity, position.Direction == PositionDirection.Short); // to use close price
            fplData.MarginInit =
                Math.Round(Math.Abs(position.Volume) * fplData.MarginRate / tradingInstrument.LeverageInit,
                    fplData.AccountBaseAssetAccuracy);
            fplData.MarginMaintenance =
                Math.Round(Math.Abs(position.Volume) * fplData.MarginRate / tradingInstrument.LeverageMaintenance,
                    fplData.AccountBaseAssetAccuracy);
        }
    }
}
