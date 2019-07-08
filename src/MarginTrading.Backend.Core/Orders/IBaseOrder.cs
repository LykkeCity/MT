// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
