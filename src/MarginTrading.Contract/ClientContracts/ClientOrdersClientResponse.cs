﻿namespace MarginTrading.Contract.ClientContracts
{
    public class ClientOrdersClientResponse
    {
        public OrderClientContract[] Positions { get; set; }
        public OrderClientContract[] Orders { get; set; }
    }
}
