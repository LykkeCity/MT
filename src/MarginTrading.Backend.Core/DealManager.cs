// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Lykke.Snow.Common.Model;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core
{
    public sealed class DealManager
    {
        private readonly OrderFulfillmentPlan _orderFulfillmentPlan;

        public DealManager(OrderFulfillmentPlan orderFulfillmentPlan)
        {
            _orderFulfillmentPlan =
                orderFulfillmentPlan ?? throw new ArgumentNullException(nameof(orderFulfillmentPlan));
        }

        /// <summary>
        /// Checks if the order can be fulfilled according to the limits
        /// </summary>
        /// <param name="oneTimeLimit">Configured limit for the deal on asset level</param>
        /// <param name="totalLimit">Configured limit for total positions on asset level</param>
        /// <param name="assetContractSize">Asset contract size</param>
        /// <param name="existingPositions">Existing open positions for the asset</param>
        /// <returns></returns>
        [Pure]
        public Result<bool, OrderLimitValidationError> SatisfiesLimits(decimal oneTimeLimit,
            decimal totalLimit,
            int assetContractSize,
            ICollection<Position> existingPositions)
        {
            // TODO: this validation is probably not related to limits validation
            if (!_orderFulfillmentPlan.RequiresPositionOpening)
                return new Result<bool, OrderLimitValidationError>(true);
            
            var unfulfilledAbsVolume = Math.Abs(_orderFulfillmentPlan.UnfulfilledVolume);
            if (oneTimeLimit > 0 && unfulfilledAbsVolume > (oneTimeLimit * assetContractSize))
            {
                return new Result<bool, OrderLimitValidationError>(OrderLimitValidationError.OneTimeLimit);
            }

            var positionsAbsVolume = existingPositions.Sum(o => Math.Abs(o.Volume));
            var oppositePositionsToBeClosedAbsVolume =
                Math.Abs(_orderFulfillmentPlan.Order.Volume) - unfulfilledAbsVolume;
            if (totalLimit > 0 &&
                (positionsAbsVolume - oppositePositionsToBeClosedAbsVolume + unfulfilledAbsVolume) >
                (totalLimit * assetContractSize))
            {
                return new Result<bool, OrderLimitValidationError>(OrderLimitValidationError.TotalLimit);
            }

            return new Result<bool, OrderLimitValidationError>(true);
        }
    }
}