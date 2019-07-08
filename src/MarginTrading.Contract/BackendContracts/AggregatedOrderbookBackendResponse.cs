// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.BackendContracts
{
    public class AggregatedOrderbookBackendResponse
    {
        public AggregatedOrderBookItemBackendContract[] Buy { get; set; }
        public AggregatedOrderBookItemBackendContract[] Sell { get; set; }
    }
}
