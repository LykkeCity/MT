// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.BackendContracts
{
    public class AggregatedOrderbookBackendResponse
    {
        public AggregatedOrderBookItemBackendContract[] Buy { get; set; }
        public AggregatedOrderBookItemBackendContract[] Sell { get; set; }
    }
}
