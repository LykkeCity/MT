using System;

namespace MarginTrading.Contract.BackendContracts.AccountsManagement
{
    public class AccountsMarginLevelResponse
    {
        public AccountsMarginLevelContract[] Levels { get; set; }
        
        public DateTime DateTime { get; set; }
    }
}