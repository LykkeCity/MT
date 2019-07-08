// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Contract.ClientContracts
{
    public class UserUpdateEntityBackendContract
    {
        public string[] ClientIds { get; set; }
        public bool UpdateAccountAssetPairs { get; set; }
        public bool UpdateAccounts { get; set; }
    }
}