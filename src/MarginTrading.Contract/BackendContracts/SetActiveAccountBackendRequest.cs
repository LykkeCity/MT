// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Contract.BackendContracts
{
    public class SetActiveAccountBackendRequest
    {
        public string AccountId { get; set; }
        public string ClientId { get; set; }
    }
}
