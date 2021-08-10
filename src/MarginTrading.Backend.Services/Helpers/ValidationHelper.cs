// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services.Helpers
{
    public static class ValidationHelper
    {
        public static void ValidateAccountId(Order order, string accountId)
        {
            if (string.IsNullOrEmpty(accountId))
                // TODO: Ensures backwards compatibility with Donut. Remove when Donut is updated
                return;

            if (order.AccountId != accountId)
                throw new InvalidOperationException(
                    $"Order {order.Id} was created by {order.AccountId}, but is being modified by {accountId}");
        }

        public static void ValidateAccountId(Position position, string accountId)
        {
            if (string.IsNullOrEmpty(accountId))
                // TODO: Ensures backwards compatibility with Donut. Remove when Donut is updated
                return;

            if (position.AccountId != accountId)
                throw new InvalidOperationException(
                    $"Position {position.Id} was created by {position.AccountId}, but is being modified by {accountId}");
        }

        public static void ValidateAccountId(PositionsCloseData positionData, string accountId)
        {
            if (string.IsNullOrEmpty(accountId))
                // TODO: Ensures backwards compatibility with Donut. Remove when Donut is updated
                return;

            if (positionData.AccountId != accountId)
                throw new InvalidOperationException(
                    $"Position group {positionData.AssetPairId} was created by {positionData.AccountId}, but is being modified by {accountId}");
        }
    }
}