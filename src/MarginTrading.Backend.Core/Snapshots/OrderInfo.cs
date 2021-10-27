// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.Orders;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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

        public OrderInfo(string id, decimal volume, decimal? expectedOpenPrice, OrderStatus status, OrderType type)
        {
            Id = id;
            Volume = volume;
            ExpectedOpenPrice = expectedOpenPrice;
            Status = status;
            Type = type;
        }

        public string Id { get; set; }

        public decimal Volume { get; set; }

        public decimal? ExpectedOpenPrice { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public OrderStatus Status { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public OrderType Type { get; set; }
    }
}