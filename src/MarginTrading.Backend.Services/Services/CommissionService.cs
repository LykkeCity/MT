// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services
{
    [UsedImplicitly]
    public class CommissionService : ICommissionService
    {
        private readonly ITradingInstrumentsCacheService _accountAssetsCacheService;
        private readonly ICfdCalculatorService _cfdCalculatorService;

        public CommissionService(
            ITradingInstrumentsCacheService accountAssetsCacheService,
            ICfdCalculatorService cfdCalculatorService)
        {
            _accountAssetsCacheService = accountAssetsCacheService;
            _cfdCalculatorService = cfdCalculatorService;
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
                result = Math.Round(swaps, AssetsConstants.DefaultAssetAccuracy);
            }

            return result;
        }

        public decimal GetSwaps(Position order)
        {
            return GetSwaps(order.AccountAssetId, order.AssetPairId, order.OpenDate, order.CloseDate,
                Math.Abs(order.Volume), order.SwapCommissionRate, order.LegalEntity);
        }

        public decimal GetOvernightSwap(Position order, decimal swapRate)
        {
            var openDate = DateTime.UtcNow;
            var closeDate = openDate.AddDays(1);
            return GetSwaps(order.AccountAssetId, order.AssetPairId, openDate, closeDate,
                Math.Abs(order.Volume), swapRate, order.LegalEntity);
        }

        public void SetCommissionRates(string tradingConditionId, Position position)
        {
            var tradingInstrument = _accountAssetsCacheService
                .GetTradingInstrument(tradingConditionId, position.AssetPairId);

            //TODO: understand what to do with comissions
        }
    }
}