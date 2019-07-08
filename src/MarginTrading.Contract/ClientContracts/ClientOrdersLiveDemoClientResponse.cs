// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.ClientContracts
{
    public class ClientOrdersLiveDemoClientResponse
    {
        public OrderClientContract[] Live { get; set; }
        public OrderClientContract[] Demo { get; set; }
    }
}