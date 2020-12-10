// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
            string tradingConditionId, decimal volume, decimal openPrice, decimal openFxPrice = 1)
        {
            return new Position(Guid.NewGuid().ToString("N"), 0, assetPairId, volume, account.Id, tradingConditionId,
                account.BaseAssetId, null, MatchingEngineConstants.DefaultMm, DateTime.UtcNow, "OpenTrade", OrderType
                .Market, volume, openPrice, openFxPrice, "USD", openPrice,
                new List<RelatedOrderInfo>(), "LYKKETEST", OriginatorType.Investor, "", assetPairId, FxToAssetPairDirection.Straight, "", false);
        }//todo assetPairId is used as FxAssetPairId which is not very correct
        
        public static Order CreateNewOrder(OrderType orderType, string assetPairId, IMarginTradingAccount account,
            string tradingConditionId, decimal volume, OrderFillType fillType = OrderFillType.FillOrKill, 
            DateTime? validity = null, decimal? price = null, bool forceOpen = false, string parentOrderId = null, 
            string parentPositionId = null, DateTime? created = null)
        {
            created = created ?? DateTime.UtcNow;
            return new Order(Guid.NewGuid().ToString("N"), 0, assetPairId, volume, created.Value, created.Value,
                validity, account.Id, tradingConditionId, account.BaseAssetId, price, "EUR", fillType,
                null, "LYKKETEST", forceOpen, orderType, parentOrderId, parentPositionId, OriginatorType.Investor, 1,
                1, assetPairId, FxToAssetPairDirection.Straight, OrderStatus.Placed, null, Guid.NewGuid().ToString());
        }//todo assetPairId is used as FxAssetPairId which is not very correct
    }
}