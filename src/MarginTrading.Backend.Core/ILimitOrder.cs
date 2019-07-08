﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core
{
    public interface ILimitOrder
    {
        string MarketMakerId { get; }
        decimal Price { get; }
    }

    public class LimitOrder : BaseOrder, ILimitOrder
    {
        public string MarketMakerId { get; set; }
        public decimal Price { get; set; }
    }
}
