// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.ClientContracts
{
    public class CloseOrderClientRequest
    {
        public string AccountId { get; set; }
        public string OrderId { get; set; }
    }
}
