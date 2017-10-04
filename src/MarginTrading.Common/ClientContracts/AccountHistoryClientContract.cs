using System;
using MarginTrading.Core;

namespace MarginTrading.Common.ClientContracts
{
    public class AccountHistoryClientContract
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public string AccountId { get; set; }
        public string ClientId { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public decimal WithdrawTransferLimit { get; set; }
        public string Comment { get; set; }
        public AccountHistoryType Type { get; set; }
    }
}
