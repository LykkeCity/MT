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

        Task<(PositionCloseResult result, Order order)> ClosePositionsAsync(PositionsCloseData data, bool 
        specialLiquidationEnabled);

        [ItemNotNull]
        Task<Dictionary<string, (PositionCloseResult, Order)>> ClosePositionsGroupAsync(string accountId,
            string assetPairId, PositionDirection? direction, OriginatorType originator, string additionalInfo,
            string correlationId);

        Task<(PositionCloseResult, Order)[]> LiquidatePositionsUsingSpecialWorkflowAsync(IMatchingEngineBase me,
            string[] positionIds, string correlationId, string additionalInfo, OriginatorType originator,
            OrderModality modality);
            
        Order CancelPendingOrder(string orderId, string additionalInfo, string correlationId,
            string comment = null, OrderCancellationReason reason = OrderCancellationReason.None);
            
        Task ChangeOrderAsync(string orderId, decimal price, DateTime? validity, OriginatorType originator,
            string additionalInfo, string correlationId, bool? forceOpen = null);
            
        (bool WillOpenPosition, decimal ReleasedMargin) MatchOnExistingPositions(Order order);
        
        void ProcessExpiredOrders(DateTime operationIntervalEnd);
    }
}
