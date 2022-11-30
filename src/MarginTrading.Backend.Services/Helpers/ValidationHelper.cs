// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services.Helpers
{
    public static class ValidationHelper
    {
        public static void ValidateAccountId(Order order, string accountId)
        {
            if (order.AccountId != accountId)
            {
                throw new AccountValidationException(
                    $"Order {order.Id} was created by {order.AccountId}, but is being modified by {accountId}",
                    AccountValidationError.AccountMismatch);
            }
        }

        public static void ValidateAccountId(Position position, string accountId)
        {
            if (position.AccountId != accountId)
            {
                throw new AccountValidationException(
                    $"Position {position.Id} was created by {position.AccountId}, but is being modified by {accountId}",
                    AccountValidationError.AccountMismatch);
            }
        }

        public static void ValidateAccountId(PositionsCloseData positionData, string accountId)
        {
            if (positionData.AccountId != accountId)
            {
                throw new AccountValidationException(
                    $"Position group {positionData.AssetPairId} was created by {positionData.AccountId}, but is being modified by {accountId}",
                    AccountValidationError.AccountMismatch);
            }
        }
    }
}