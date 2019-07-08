// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.ClientContracts
{
    public class ChangeOrderLimitsRpcClientRequest : ChangeOrderLimitsClientRequest
    {
        public string Token { get; set; }
    }
}