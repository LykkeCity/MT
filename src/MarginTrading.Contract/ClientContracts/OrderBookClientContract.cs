// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.ClientContracts
{
    public class OrderBookClientContract
    {
        public OrderBookLevelClientContract[] Buy { get; set; }
        public OrderBookLevelClientContract[] Sell { get; set; }
    }
}
