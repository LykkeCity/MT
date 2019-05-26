using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core
{
    public interface ITradingEngine
    {
        Task<Order> PlaceOrderAsync(Order order);

        Task<Order> ClosePositionsAsync(PositionsCloseData data);

        Task<Order[]> LiquidatePositionsUsingSpecialWorkflowAsync(IMatchingEngineBase me, string[] positionIds,
            string correlationId, string additionalInfo, OriginatorType originator);
            
        Order CancelPendingOrder(string orderId, string additionalInfo, string correlationId,
            string comment = null, OrderCancellationReason reason = OrderCancellationReason.None);
            
        void ChangeOrder(string orderId, decimal price, DateTime? validity, OriginatorType originator,
            string additionalInfo, string correlationId, bool? forceOpen = null);
            
        bool ShouldOpenNewPosition(Order order);
        
        void ProcessExpiredOrders(DateTime operationIntervalEnd);
    }
}
