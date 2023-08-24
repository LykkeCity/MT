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
        /// <param name="maxPositionNotional">Configured limit for max position notional</param>
        /// <param name="assetContractSize">Asset contract size</param>
        /// <param name="existingPositions">Existing open positions for the asset</param>
        /// <param name="quote">Quote with bid/ask prices</param>
        /// <returns></returns>
        [Pure]
        public Result<bool, OrderLimitValidationError> SatisfiesLimits(decimal oneTimeLimit,
            decimal totalLimit,
            decimal? maxPositionNotional,
            int assetContractSize,
            ICollection<Position> existingPositions,
            InstrumentBidAskPair quote)
        {
            // TODO: this validation is probably not related to limits validation
            if (!_orderFulfillmentPlan.RequiresPositionOpening)
                return new Result<bool, OrderLimitValidationError>(true);
            
            var unfulfilledAbsVolume = Math.Abs(_orderFulfillmentPlan.UnfulfilledVolume);
            if (oneTimeLimit > 0 && unfulfilledAbsVolume > (oneTimeLimit * assetContractSize))
            {
                return new Result<bool, OrderLimitValidationError>(OrderLimitValidationError.OneTimeLimit);
            }
            
            var order = _orderFulfillmentPlan.Order;
            var positionsAbsVolume = existingPositions.Sum(o => Math.Abs(o.Volume));
            var oppositePositionsToBeClosedAbsVolume =
                Math.Abs(order.Volume) - unfulfilledAbsVolume;
            if (totalLimit > 0 &&
                (positionsAbsVolume - oppositePositionsToBeClosedAbsVolume + unfulfilledAbsVolume) >
                (totalLimit * assetContractSize))
            {
                return new Result<bool, OrderLimitValidationError>(OrderLimitValidationError.TotalLimit);
            }

            if (maxPositionNotional.HasValue)
            {
                var sameAsOrderDirectionPositionsAbsVolume = existingPositions
                    .Where(o => o.Direction == order.Direction.GetOpenPositionDirection())
                    .Sum(o => Math.Abs(o.Volume));
                var oppositeAsOrderDirectionPositionsAbsVolume = existingPositions
                    .Where(o => o.Direction == order.Direction.GetClosePositionDirection())
                    .Sum(o => Math.Abs(o.Volume));
                var fxRate = order.FxRate;
                var priceSameDirection = quote.GetPriceForOrderDirection(order.Direction);
                var priceOppositeDirection = quote.GetPriceForOrderDirection(order.Direction.GetOpositeDirection());
                var notionalBeforeEur = (sameAsOrderDirectionPositionsAbsVolume * priceOppositeDirection +
                                      oppositeAsOrderDirectionPositionsAbsVolume * priceSameDirection) * fxRate;
                var notionalDeltaEur = (unfulfilledAbsVolume * priceOppositeDirection -
                                        oppositePositionsToBeClosedAbsVolume * priceSameDirection) * fxRate;
                var notionalAfterEur = notionalBeforeEur + notionalDeltaEur;
                if (notionalAfterEur > maxPositionNotional && notionalAfterEur >= notionalBeforeEur)
                {
                    return new Result<bool, OrderLimitValidationError>(OrderLimitValidationError.MaxPositionNotionalLimit);
                }
            }

            return new Result<bool, OrderLimitValidationError>(true);
        }
    }
}