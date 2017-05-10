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
        public double Amount { get; set; }
        public double Balance { get; set; }
        public string Comment { get; set; }
        public AccountHistoryType Type { get; set; }
    }
}
