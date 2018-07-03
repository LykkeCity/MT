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
        private readonly ITradingInstrumentsCacheService _accountAssetsCacheService;
        private readonly IAssetsCache _assetsCache;

        public FplService(
            ICfdCalculatorService cfdCalculatorService,
            IAssetPairsCache assetPairsCache,
            IAccountsCacheService accountsCacheService,
            ITradingInstrumentsCacheService accountAssetsCacheService,
            IAssetsCache assetsCache)
        {
            _cfdCalculatorService = cfdCalculatorService;
            _assetPairsCache = assetPairsCache;
            _accountsCacheService = accountsCacheService;
            _accountAssetsCacheService = accountAssetsCacheService;
            _assetsCache = assetsCache;
        }

        public void UpdateOrderFpl(Position order, FplData fplData)
        {
//            var handler = order.Status != OrderStatus.WaitingForExecution
//                ? UpdateOrderFplData
//                : (Action<IOrder, FplData>)UpdatePendingOrderMargin;
//
//            handler(order, fplData);

            UpdatePositionFplData(order, fplData);

        }

        public decimal GetInitMarginForOrder(Order order)
        {
            var accountAsset =
                _accountAssetsCacheService.GetTradingInstrument(order.TradingConditionId, order.AssetPairId);
            var marginRate = _cfdCalculatorService.GetQuoteRateForBaseAsset(order.AccountAssetId, order.AssetPairId,
                order.LegalEntity);
            var accountBaseAssetAccuracy = _assetsCache.GetAssetAccuracy(order.AccountAssetId);

            return Math.Round(Math.Abs(order.Volume) * marginRate / accountAsset.LeverageInit,
                accountBaseAssetAccuracy);

        }

        private void UpdatePositionFplData(Position position, FplData fplData)
        {
            fplData.AccountBaseAssetAccuracy = _assetsCache.GetAssetAccuracy(position.AccountAssetId);
            fplData.FplRate = _cfdCalculatorService.GetQuoteRateForQuoteAsset(position.AccountAssetId, position.AssetPairId, 
                position.LegalEntity, position.Volume * (position.ClosePrice - position.OpenPrice) > 0);

            var fpl = (position.ClosePrice - position.OpenPrice) * fplData.FplRate * position.Volume;

            fplData.Fpl = Math.Round(fpl, fplData.AccountBaseAssetAccuracy);

            fplData.OpenPrice = position.OpenPrice;
            fplData.ClosePrice = position.ClosePrice;
            
            CalculateMargin(position, fplData);
            
            fplData.SwapsSnapshot = position.GetSwaps();
            
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

        public void CalculateMargin(Position order, FplData fplData)
        {
            var accountAsset =
                _accountAssetsCacheService.GetTradingInstrument(order.TradingConditionId, order.AssetPairId);

//            fplData.MarginRate = _cfdCalculatorService.GetQuoteRateForBaseAsset(order.AccountAssetId, order.Instrument, 
//                order.LegalEntity);
//            fplData.MarginInit =
//                Math.Round(Math.Abs(order.Volume) * fplData.MarginRate / accountAsset.LeverageInit,
//                    fplData.AccountBaseAssetAccuracy);
//            fplData.MarginMaintenance =
//                Math.Round(Math.Abs(order.Volume) * fplData.MarginRate / accountAsset.LeverageMaintenance,
//                    fplData.AccountBaseAssetAccuracy);
            
            fplData.MarginRate = fplData.ClosePrice * fplData.FplRate;
            fplData.MarginInit =
                Math.Round(Math.Abs(order.Volume) * fplData.MarginRate / accountAsset.LeverageInit,
                    fplData.AccountBaseAssetAccuracy);
            fplData.MarginMaintenance =
                Math.Round(Math.Abs(order.Volume) * fplData.MarginRate / accountAsset.LeverageMaintenance,
                    fplData.AccountBaseAssetAccuracy);
        }
    }
}
