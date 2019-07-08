// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.BackendContracts
{
    public class ClientOrdersBackendResponse
    {
        public OrderBackendContract[] Positions { get; set; }
        public OrderBackendContract[] Orders { get; set; }
    }
}
