using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTradingTests.Helpers
{
    public static class TestObjectsFactory
    {
        public static Position CreateOpenedPosition(string assetPairId, IMarginTradingAccount account,
            string tradingConditionId, decimal volume, decimal openPrice)
        {
            return new Position(Guid.NewGuid().ToString("N"), 0, assetPairId, volume, account.Id, tradingConditionId,
                account.BaseAssetId, null, MatchingEngineConstants.DefaultMm, DateTime.UtcNow, "OpenTrade", openPrice, 1, "USD", openPrice,
                new List<RelatedOrderInfo>(), "LYKKETEST", OriginatorType.Investor, "");
        }
        
        public static Order CreateNewOrder(OrderType orderType, string assetPairId, IMarginTradingAccount account,
            string tradingConditionId, decimal volume, OrderFillType fillType = OrderFillType.FillOrKill, DateTime? validity = null, decimal? price = null,
            bool forceOpen = false, string parentOrderId = null, string parentPositionId = null)
        {
            return new Order(Guid.NewGuid().ToString("N"), 0, assetPairId, volume, DateTime.UtcNow, DateTime.UtcNow,
                validity, account.Id, tradingConditionId, account.BaseAssetId, price, "EUR", fillType,
                null, "LYKKETEST", forceOpen, orderType, parentOrderId, parentPositionId, OriginatorType.Investor, 1,
                1, OrderStatus.Placed, null);
        }
    }
}