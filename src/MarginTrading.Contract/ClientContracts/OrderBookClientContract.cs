// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.ClientContracts
{
    public class OrderBookClientContract
    {
        public OrderBookLevelClientContract[] Buy { get; set; }
        public OrderBookLevelClientContract[] Sell { get; set; }
    }
}
