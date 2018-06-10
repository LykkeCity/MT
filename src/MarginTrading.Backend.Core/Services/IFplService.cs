using System.Collections.Generic;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core
{
    public interface IFplService
    {
        void UpdateOrderFpl(IPosition order, FplData fplData);
        decimal GetMatchedOrdersPrice(List<MatchedOrder> matchedOrders, string instrument);
        void CalculateMargin(IPosition order, FplData fplData);
    }
}
