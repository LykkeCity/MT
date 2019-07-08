// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.ClientContracts
{
    public class ClientPositionsLiveDemoClientResponse
    {
        public ClientOrdersClientResponse Live { get; set; }
        public ClientOrdersClientResponse Demo { get; set; }
    }
}