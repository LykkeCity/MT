using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core
{
    public interface IFplService
    {
        void UpdateOrderFpl(Position order, FplData fplData);
        decimal GetInitMarginForOrder(Order order);
    }
}
