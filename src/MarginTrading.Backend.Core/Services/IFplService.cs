using System.Collections.Generic;
using MarginTrading.Backend.Core.MatchedOrders;

namespace MarginTrading.Backend.Core
{
    public interface IFplService
    {
        void UpdateOrderFpl(IOrder order, FplData fplData);
        void UpdatePendingOrderMargin(IOrder order, FplData fplData);
        decimal GetMatchedOrdersPrice(List<MatchedOrder> matchedOrders, string instrument);
        void CalculateMargin(IOrder order, FplData fplData);
    }
}
