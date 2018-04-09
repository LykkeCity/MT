using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services
{
    public class FplService : IFplService
    {
        private readonly ICfdCalculatorService _cfdCalculatorService;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IAccountAssetsCacheService _accountAssetsCacheService;
        private readonly IAssetsCache _assetsCache;

        public FplService(
            ICfdCalculatorService cfdCalculatorService,
            IAssetPairsCache assetPairsCache,
            IAccountsCacheService accountsCacheService,
            IAccountAssetsCacheService accountAssetsCacheService,
            IAssetsCache assetsCache)
        {
            _cfdCalculatorService = cfdCalculatorService;
            _assetPairsCache = assetPairsCache;
            _accountsCacheService = accountsCacheService;
            _accountAssetsCacheService = accountAssetsCacheService;
            _assetsCache = assetsCache;
        }

        public void UpdateOrderFpl(IOrder order, FplData fplData)
        {
            fplData.AccountBaseAssetAccuracy = _assetsCache.GetAssetAccuracy(order.AccountAssetId);
            fplData.QuoteRate = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.AccountAssetId, order.Instrument, order.LegalEntity);

            var fpl = order.GetOrderType() == OrderDirection.Buy
                ? (order.ClosePrice - order.OpenPrice) * fplData.QuoteRate * order.GetMatchedVolume()
                : (order.OpenPrice - order.ClosePrice) * fplData.QuoteRate * order.GetMatchedVolume();

            fplData.Fpl = Math.Round(fpl, fplData.AccountBaseAssetAccuracy);

            var accountAsset = _accountAssetsCacheService.GetAccountAsset(order.TradingConditionId, order.AccountAssetId, order.Instrument);

            fplData.MarginInit = Math.Round(order.ClosePrice * order.GetMatchedVolume() * fplData.QuoteRate / accountAsset.LeverageInit, fplData.AccountBaseAssetAccuracy);
            fplData.MarginMaintenance = Math.Round(order.ClosePrice * order.GetMatchedVolume() * fplData.QuoteRate / accountAsset.LeverageMaintenance, fplData.AccountBaseAssetAccuracy);

            fplData.OpenCrossPrice = Math.Round(order.OpenPrice * fplData.QuoteRate, order.AssetAccuracy);
            fplData.CloseCrossPrice = Math.Round(order.ClosePrice * fplData.QuoteRate, order.AssetAccuracy);

            fplData.OpenPrice = order.OpenPrice;
            fplData.ClosePrice = order.ClosePrice;
            fplData.SwapsSnapshot = order.GetSwaps();
            
            fplData.CalculatedHash = fplData.ActualHash;
            
            fplData.TotalFplSnapshot = order.GetTotalFpl(fplData.SwapsSnapshot);

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            account.CacheNeedsToBeUpdated();
        }

        public decimal GetMatchedOrdersPrice(List<MatchedOrder> matchedOrders, string instrument)
        {
            if (matchedOrders.Count == 0)
            {
                return 0;
            }

            var accuracy = _assetPairsCache.GetAssetPairById(instrument).Accuracy;

            return Math.Round(matchedOrders.Sum(item => item.Price * item.Volume) /
                              matchedOrders.Sum(item => item.Volume), accuracy);
        }
    }
}
