using System;

namespace MarginTrading.AccountHistoryBroker.Repositories.Models
{
    public class AccountTransactionsReport : IAccountTransactionsReport
    {
        public string AccountId { get; set; }
        public double Amount { get; set; }
        public double Balance { get; set; }
        public string ClientId { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; }
        public string Id { get; set; }
        public string PositionId { get; set; }
        public string Type { get; set; }
        public double WithdrawTransferLimit { get; set; }
        public string LegalEntity { get; set; }
        public string AuditLog { get; set; }
    }
}
