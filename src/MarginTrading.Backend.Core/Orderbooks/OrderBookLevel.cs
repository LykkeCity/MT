﻿namespace MarginTrading.Backend.Core.Orderbooks
{
    public class OrderBookLevel
    {
        public string Instrument { get; set; }
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public OrderDirection Direction { get; set; }

        public static OrderBookLevel Create(LimitOrder order)
        {
            return new OrderBookLevel
            {
                Direction = order.GetOrderType(),
                Instrument = order.Instrument,
                Volume = order.Volume,
                Price = order.Price
            };
        }

        public static OrderBookLevel Create(LimitOrder order, OrderDirection direction)
        {
            return new OrderBookLevel
            {
                Direction = direction,
                Instrument = order.Instrument,
                Volume = order.Volume,
                Price = order.Price
            };
        }

        public static OrderBookLevel CreateDeleted(LimitOrder order)
        {
            return new OrderBookLevel
            {
                Direction = order.GetOrderType(),
                Instrument = order.Instrument,
                Volume = 0,
                Price = order.Price
            };
        }
    }
}