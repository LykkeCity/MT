using System.Collections.Generic;

namespace MarginTrading.Core
{
    public interface IFplService
    {
        void UpdateOrderFpl(IOrder order, FplData fplData);
        double GetMatchedOrdersPrice(List<MatchedOrder> matchedOrders, string instrument);
    }
}
