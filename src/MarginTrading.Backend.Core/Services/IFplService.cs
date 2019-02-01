using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core
{
    public interface IFplService
    {
        void UpdatePositionFpl(Position order);
        
        decimal GetInitMarginForOrder(Order order);

        decimal CalculateOvernightMaintenanceMargin(Position position);
    }
}
