// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.ClientContracts
{
    public class OpenOrderRpcClientRequest
    {
        public string Token { get; set; }
        public NewOrderClientContract Order { get; set; }
    }
}
