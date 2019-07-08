// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace MarginTrading.Contract.ClientContracts
{
    [DisplayName("User update info")]
    public class UserUpdateEntityClientContract
    {
        [DisplayName("Indicates account assets were updated")]
        public bool UpdateAccountAssetPairs { get; set; }
        [DisplayName("Indicates accounts were updated")]
        public bool UpdateAccounts { get; set; }
    }
}