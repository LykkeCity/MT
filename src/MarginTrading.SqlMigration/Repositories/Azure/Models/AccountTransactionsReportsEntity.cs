using MarginTrading.AccountHistoryBroker.Repositories.Models;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace MarginTrading.SqlMigration.Repositories.Azure.Models
{
    internal class AccountTransactionsReportsEntity : TableEntity, IAccountTransactionsReport
    {
        public string Id
        {
            get => RowKey;
            set => RowKey = value;
        }

        public string AccountId
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public double Amount { get; set; }
        decimal IAccountTransactionsReport.Amount => (decimal)Amount;
        public double Balance { get; set; }
        decimal IAccountTransactionsReport.Balance => (decimal)Balance;
        public string ClientId { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public double WithdrawTransferLimit { get; set; }
        decimal IAccountTransactionsReport.WithdrawTransferLimit => (decimal)WithdrawTransferLimit;
        public string PositionId { get; set; }
    }
}
