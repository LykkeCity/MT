// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTradingTests.Helpers
{
    public static class DumbDataGenerator
    {
        public static Position GeneratePosition(string id = null,
            string assetPairId = null,
            decimal? volume = null,
            decimal? margin = null,
            string accountId = null,
            decimal? openPrice = null)
        {
            var result = new Position(
                id ?? "1",
                1,
                assetPairId ?? string.Empty,
                volume ?? default,
                accountId ?? string.Empty,
                string.Empty,
                string.Empty,
                null,
                string.Empty,
                default,
                string.Empty,
                default,
                default,
                openPrice ?? default,
                default,
                string.Empty,
                default,
                new List<RelatedOrderInfo>(),
                string.Empty,
                default,
                string.Empty,
                string.Empty,
                default,
                string.Empty,
                default);

            result.FplData.CalculatedHash = 1;
            result.FplData.MarginMaintenance = margin ?? default;

            return result;
        }

        public static Order GenerateOrder(string assetPairId = null,
            decimal? volume = null,
            string accountId = null,
            List<string> positionsToBeClosed = null,
            OrderStatus? status = null) =>
            new Order(
                "1",
                default,
                assetPairId ?? string.Empty,
                volume ?? default,
                default,
                default,
                null,
                accountId ?? string.Empty,
                string.Empty,
                string.Empty,
                default,
                string.Empty,
                default,
                string.Empty,
                string.Empty,
                default,
                default,
                string.Empty,
                string.Empty,
                default,
                default,
                default,
                string.Empty,
                default,
                status ?? OrderStatus.Placed,
                string.Empty,
                positionsToBeClosed);
    }
}