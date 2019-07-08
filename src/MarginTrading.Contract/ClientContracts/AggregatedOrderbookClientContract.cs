// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.ClientContracts
{
    public class AggregatedOrderbookClientContract
    {
        public AggregatedOrderBookItemClientContract[] Buy { get; set; }
        public AggregatedOrderBookItemClientContract[] Sell { get; set; }
    }
}
