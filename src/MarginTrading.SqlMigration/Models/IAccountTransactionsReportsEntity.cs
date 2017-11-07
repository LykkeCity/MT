using System;

namespace MarginTrading.SqlMigration.Models
{
    internal interface IAccountTransactionsReports
    {
        string AccountId { get; }
        decimal Amount { get; }
        decimal Balance { get; }
        string ClientId { get; }
        string Comment { get; }
        DateTime Date { get; }
        string Id { get; }
        string PositionId { get; }
        string Type { get; }
        decimal WithdrawTransferLimit { get; }
    }
}