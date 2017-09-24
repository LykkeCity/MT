using System;
using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class CommissionService : ICommissionService
    {
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IAccountAssetsCacheService _accountAssetsCacheService;
        private readonly ICfdCalculatorService _calculator;

        public CommissionService(
            IAssetPairsCache assetPairsCache,
            IAccountAssetsCacheService accountAssetsCacheService,
            ICfdCalculatorService calculator)
        {
            _assetPairsCache = assetPairsCache;
            _accountAssetsCacheService = accountAssetsCacheService;
            _calculator = calculator;
        }

        private decimal GetSwaps(string accountAssetId, string instrument, OrderDirection type, DateTime? openDate, DateTime? closeDate, decimal volume, decimal swapRate)
        {
            decimal result = 0;

            if (openDate.HasValue)
            {
                var asset = _assetPairsCache.GetAssetPairById(instrument);
                var close = closeDate ?? DateTime.UtcNow;
                var seconds = (decimal)(close - openDate.Value).TotalSeconds;

                const int secondsInYear = 31536000;
                var volumeInAccAsset = _calculator.GetVolumeInAccountAsset(type, accountAssetId, instrument, volume);
                var swaps = volumeInAccAsset * swapRate * seconds / secondsInYear;
                result = Math.Round(swaps, asset.Accuracy);
            }

            return result;
        }

        public decimal GetSwaps(IOrder order)
        {
            return GetSwaps(order.AccountAssetId, order.Instrument,
                order.GetOrderType(), order.OpenDate, order.CloseDate, order.GetMatchedVolume(), order.SwapCommission);
        }

        public void SetCommissionRates(string tradingConditionId, string accountAssetId, Order order)
        {
            var accountAsset = _accountAssetsCacheService.GetAccountAsset(tradingConditionId, accountAssetId, order.Instrument);

            order.CommissionLot = accountAsset.CommissionLot;

            switch (order.GetOrderType())
            {
                case OrderDirection.Buy:
                    order.OpenCommission = accountAsset.CommissionLong;
                    order.CloseCommission = accountAsset.CommissionShort;
                    order.SwapCommission = accountAsset.SwapLong;
                    break;
                case OrderDirection.Sell:
                    order.OpenCommission = order.GetOrderType() == OrderDirection.Buy ? accountAsset.CommissionLong : accountAsset.CommissionShort;
                    order.CloseCommission = order.GetOrderType() == OrderDirection.Buy ? accountAsset.CommissionShort : accountAsset.CommissionLong;
                    order.SwapCommission = accountAsset.SwapShort;
                    break;
            }
        }
    }
}
