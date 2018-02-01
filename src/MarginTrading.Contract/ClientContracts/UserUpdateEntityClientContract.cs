using System.ComponentModel;

namespace MarginTrading.Contract.ClientContracts
{
    [DisplayName("User update info")]
    public class UserUpdateEntityClientContract
    {
        [DisplayName("Inicates account assets were updated")]
        public bool UpdateAccountAssetPairs { get; set; }
        [DisplayName("Inicates accounts were updated")]
        public bool UpdateAccounts { get; set; }
    }
}