using System.Threading.Tasks;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core
{
    public interface ITradingEngine
    {
        Task<Order> PlaceOrderAsync(Order order);

        Task<Order> ClosePositionAsync(string positionId, OriginatorType originator, string additionalInfo,
            string correlationId, string comment = null, IMatchingEngineBase me = null, 
            OrderModality modality = OrderModality.Regular);

        Task<Order[]> LiquidatePositionsAsync(IMatchingEngineBase me, string[] positionIds,
            string correlationId);
            
        Order CancelPendingOrder(string orderId, OriginatorType originator, string additionalInfo, string correlationId,
            string comment = null);
            
        void ChangeOrderLimits(string orderId, decimal price, OriginatorType originator, string additionalInfo,
            string correlationId);
            
        bool PingLock();
        
        bool ShouldOpenNewPosition(Order order);
    }
}
