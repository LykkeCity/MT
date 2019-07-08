// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.BackendContracts
{
    public class AggregatedOrderBookItemBackendContract
    {
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
    }
}