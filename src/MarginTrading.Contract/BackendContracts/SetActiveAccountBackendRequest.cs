// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.BackendContracts
{
    public class SetActiveAccountBackendRequest
    {
        public string AccountId { get; set; }
        public string ClientId { get; set; }
    }
}
