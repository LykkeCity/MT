// Copyright (c) 2019 Lykke Corp.

using System;

namespace MarginTrading.Contract.BackendContracts
{
    public class GraphBidAskPairBackendContract
    {
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public DateTime Date { get; set; }
    }
}
