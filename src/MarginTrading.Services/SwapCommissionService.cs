using System;
using MarginTrading.Core;
using MarginTrading.Services.Helpers;

namespace MarginTrading.Services
{
    public class SwapCommissionService : ISwapCommissionService
    {
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IAccountAssetsCacheService _accountAssetsCacheService;
        private readonly ICfdCalculatorService _calculator;

        public SwapCommissionService(
            IAssetPairsCache assetPairsCache,
            IAccountAssetsCacheService accountAssetsCacheService,
            ICfdCalculatorService calculator)
        {
            _assetPairsCache = assetPairsCache;
            _accountAssetsCacheService = accountAssetsCacheService;
            _calculator = calculator;
        }

        public decimal GetSwapCount(DateTime startDate, DateTime endDate)
        {
            var delta = startDate <= startDate.Date.AddHours(21) && endDate >= endDate.Date.AddHours(21) ? 1 : 0;

            return (int)(endDate - startDate).TotalHours / 24 + delta;
        }

        public decimal GetSwaps(string tradingConditionId, string accountId, string accountAssetId, string instrument, OrderDirection type, DateTime? openDate, DateTime? closeDate, decimal volume)
        {
            decimal result = 0;

            if (openDate.HasValue)
            {
                var asset = _assetPairsCache.GetAssetPairById(instrument);

                var accountAsset = _accountAssetsCacheService.GetAccountAsset(tradingConditionId, accountAssetId, instrument);

                var close = closeDate ?? DateTime.UtcNow;
                var seconds = (decimal)(close - openDate.Value).TotalSeconds;

                var swaps = type == OrderDirection.Buy ? accountAsset.SwapLong : accountAsset.SwapShort;
                var swapsPct = type == OrderDirection.Buy ? accountAsset.SwapLongPct : accountAsset.SwapShortPct;

                var vol = _calculator.GetVolumeInAccountAsset(type, accountAssetId, instrument, volume);
                const int secondsInYear = 31536000;
                var swapsVolume = MarginTradingCalculations.GetVolumeFromPoints(swaps * seconds / secondsInYear, asset.Accuracy);
                var swapsVolumePct = swapsPct * seconds * vol / secondsInYear;
                result = Math.Round(swapsVolume + swapsVolumePct, asset.Accuracy);
            }

            return result;
        }

        //TODO: fix swap calculation algorithm and performance in task LWDEV-3087
        public decimal GetSwaps(IOrder order)
        {
            return 0;
            //return GetSwaps(order.TradingConditionId, order.AccountId, order.AccountAssetId, order.Instrument,
            //    order.GetOrderType(), order.OpenDate, order.CloseDate, order.GetMatchedVolume());
        }

        public void SetCommissions(string tradingConditionId, string accountAssetId, Order order)
        {
            var accountAsset = _accountAssetsCacheService.GetAccountAsset(tradingConditionId, accountAssetId, order.Instrument);
            order.OpenCommission = order.GetOrderType() == OrderDirection.Buy ? accountAsset.CommissionLong : accountAsset.CommissionShort;
            order.CloseCommission = order.GetOrderType() == OrderDirection.Buy ? accountAsset.CommissionShort : accountAsset.CommissionLong;
            order.CommissionLot = accountAsset.CommissionLot;
        }
    }
}
