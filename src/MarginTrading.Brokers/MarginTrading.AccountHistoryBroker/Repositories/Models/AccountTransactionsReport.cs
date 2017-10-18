using MarginTrading.Core;
using System;

namespace MarginTrading.AccountHistoryBroker.Repositories.Models
{
    internal class AccountTransactionsReport : IAccountTransactionsReport
    {
        public string AccountId { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public string ClientId { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; }
        public string Id { get; set; }
        public string PositionId { get; set; }
        public string Type { get; set; }
        public decimal WithdrawTransferLimit { get; set; }
    }
}
