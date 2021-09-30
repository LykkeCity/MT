// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Snapshots
{
    /// <summary>
    /// Represents short information of an order that used for state validation.
    /// </summary>
    public class OrderInfo
    {
        public OrderInfo()
        {
        }

        public OrderInfo(string id, decimal volume, decimal? expectedOpenPrice, OrderStatus status)
        {
            Id = id;
            Volume = volume;
            ExpectedOpenPrice = expectedOpenPrice;
            Status = status;
        }

        public string Id { get; set; }

        public decimal Volume { get; set; }

        public decimal? ExpectedOpenPrice { get; set; }

        public OrderStatus Status { get; set; }
    }
}