using System;

namespace MarginTrading.AccountHistoryBroker.Repositories.Models
{
    public interface IAccountTransactionsReport
    {
        string AccountId { get; }
        double Amount { get; }
        double Balance { get; }
        string ClientId { get; }
        string Comment { get; }
        DateTime Date { get; }
        string Id { get; }
        string PositionId { get; }
        string Type { get; }
        double WithdrawTransferLimit { get; }
        string LegalEntity { get; }
        string AuditLog { get; }
    }
}
