// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.ClientContracts
{
    public class AggregatedOrderbookClientContract
    {
        public AggregatedOrderBookItemClientContract[] Buy { get; set; }
        public AggregatedOrderBookItemClientContract[] Sell { get; set; }
    }
}
