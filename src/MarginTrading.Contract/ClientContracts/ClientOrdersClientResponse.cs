// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.ClientContracts
{
    public class ClientOrdersClientResponse
    {
        public OrderClientContract[] Positions { get; set; }
        public OrderClientContract[] Orders { get; set; }
    }
}
