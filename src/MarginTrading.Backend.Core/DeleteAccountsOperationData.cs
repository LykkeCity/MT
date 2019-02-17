using System.Collections.Generic;

namespace MarginTrading.Backend.Core
{
    public class DeleteAccountsOperationData: OperationDataBase<OperationState>
    {
        public List<string> DontUnblockAccounts { get; set; } = new List<string>();
    }
}