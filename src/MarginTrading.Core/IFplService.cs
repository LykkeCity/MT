using System.Collections.Generic;
using MarginTrading.Core.MatchedOrders;

namespace MarginTrading.Core
{
    public interface IFplService
    {
        void UpdateOrderFpl(IOrder order, FplData fplData);
        decimal GetMatchedOrdersPrice(List<MatchedOrder> matchedOrders, string instrument);
    }
}
