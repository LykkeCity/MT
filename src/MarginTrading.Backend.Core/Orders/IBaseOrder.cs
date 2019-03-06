using System;
using MarginTrading.Backend.Core.MatchedOrders;

namespace MarginTrading.Backend.Core.Orders
{
    public interface IBaseOrder
    {
        string Id { get; }
        string Instrument { get; }
        decimal Volume { get; }
        DateTime CreateDate { get; }
        MatchedOrderCollection MatchedOrders { get; set; }
    }
}
