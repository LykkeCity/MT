// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core
{
    public interface ITradingEngine
    {
        Task<Order> PlaceOrderAsync(Order order);

        Task<(PositionCloseResult result, Order order)> ClosePositionsAsync(PositionsCloseData data, bool specialLiquidationEnabled);

        /// <summary>
        /// Close group of positions by accountId, assetPairId and direction
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="assetPairId"></param>
        /// <param name="direction"></param>
        /// <param name="originator"></param>
        /// <param name="additionalInfo"></param>
        /// <returns></returns>
        Task<Dictionary<string, (PositionCloseResult, Order)>> ClosePositionsGroupAsync(string accountId,
            string assetPairId, PositionDirection? direction, OriginatorType originator, string additionalInfo);

        /// <summary>
        /// Close predefined group of positions, assuming the list of positions is of single account, instrument and direction
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="operationId"></param>
        /// <param name="direction"></param>
        /// <param name="originator"></param>
        /// <param name="additionalInfo"></param>
        /// <returns></returns>
        Task<Dictionary<string, (PositionCloseResult, Order)>> ClosePositionsGroupAsync(
            IList<Position> positions,
            string operationId,
            OriginatorType originator,
            PositionDirection? direction = null,
            string additionalInfo = null);

        Task<(PositionCloseResult, Order)[]> LiquidatePositionsUsingSpecialWorkflowAsync(IMatchingEngineBase me,
            string[] positionIds, string additionalInfo, OriginatorType originator,
            OrderModality modality);
            
        Order CancelPendingOrder(string orderId, string additionalInfo,
            string comment = null, OrderCancellationReason reason = OrderCancellationReason.None);
            
        Task ChangeOrderAsync(string orderId, decimal price, OriginatorType originator,
            string additionalInfo, bool? forceOpen = null);

        PositionsMatchingDecision MatchOnExistingPositions(Order order);

        void ProcessExpiredOrders(DateTime operationIntervalEnd);

        Task ChangeOrderValidityAsync(string orderId, DateTime validity, OriginatorType originator,
            string additionalInfo);

        Task RemoveOrderValidityAsync(string orderId, OriginatorType originator,
            string additionalInfo);
    }
}
