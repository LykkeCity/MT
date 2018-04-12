using System;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services
{
    public class CommissionService : ICommissionService
    {
        private readonly IAccountAssetsCacheService _accountAssetsCacheService;
        private readonly ICfdCalculatorService _cfdCalculatorService;
        private readonly IAssetsCache _assetsCache;

        public CommissionService(
            IAccountAssetsCacheService accountAssetsCacheService,
            ICfdCalculatorService cfdCalculatorService,
            IAssetsCache assetsCache)
        {
            _accountAssetsCacheService = accountAssetsCacheService;
            _cfdCalculatorService = cfdCalculatorService;
            _assetsCache = assetsCache;
        }

        private decimal GetSwaps(string accountAssetId, string instrument, DateTime? openDate, DateTime? closeDate,
            decimal volume, decimal swapRate, string legalEntity)
        {
            decimal result = 0;

            if (openDate.HasValue)
            {
                var close = closeDate ?? DateTime.UtcNow;
                var seconds = (decimal) (close - openDate.Value).TotalSeconds;

                const int secondsInYear = 31536000;
                var quote = _cfdCalculatorService.GetQuoteRateForBaseAsset(accountAssetId, instrument, legalEntity, 
                    volume * swapRate > 0);
                var swaps = quote * volume * swapRate * seconds / secondsInYear;
                result = Math.Round(swaps, _assetsCache.GetAssetAccuracy(accountAssetId));
            }

            return result;
        }

        public decimal GetSwaps(IOrder order)
        {
            return GetSwaps(order.AccountAssetId, order.Instrument, order.OpenDate, order.CloseDate,
                order.GetMatchedVolume(), order.SwapCommission, order.LegalEntity);
        }

        public decimal GetOvernightSwap(IOrder order, decimal swapRate)
        {
            var openDate = DateTime.UtcNow;
            var closeDate = openDate.AddDays(1);
            return GetSwaps(order.AccountAssetId, order.Instrument, openDate, closeDate,
                Math.Abs(order.Volume), swapRate, order.LegalEntity);
        }

        public void SetCommissionRates(string tradingConditionId, string accountAssetId, Order order)
        {
            var accountAsset = _accountAssetsCacheService
                .GetAccountAsset(tradingConditionId, accountAssetId, order.Instrument);

            order.CommissionLot = accountAsset.CommissionLot;

            switch (order.GetOrderType())
            {
                case OrderDirection.Buy:
                    order.OpenCommission = accountAsset.CommissionLong;
                    order.CloseCommission = accountAsset.CommissionShort;
                    order.SwapCommission = accountAsset.SwapLong;
                    break;
                case OrderDirection.Sell:
                    order.OpenCommission = accountAsset.CommissionShort;
                    order.CloseCommission = accountAsset.CommissionLong;
                    order.SwapCommission = accountAsset.SwapShort;
                    break;
            }
        }
    }
}