// Copyright (c) 2019 Lykke Corp.

using System;

namespace MarginTrading.Contract.ClientContracts
{
    public class InitPricesResponse
    {
        public DateTime ServerTime { get; set; }
        public BidAskClientContract[] Prices { get; set; }
    }
}
