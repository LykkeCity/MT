using System;
using MarginTrading.AccountHistoryBroker.Repositories.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AccountHistoryBroker.Repositories.AzureRepositories
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
        public double Balance { get; set; }
        public string ClientId { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public double WithdrawTransferLimit { get; set; }
        public string PositionId { get; set; }
        public string LegalEntity { get; set; }
        public string AuditLog { get; set; }

        public static AccountTransactionsReportsEntity Create(IAccountTransactionsReport src)
        {
            return new AccountTransactionsReportsEntity
            {
                Id = src.Id,
                Date = src.Date,
                AccountId = src.AccountId,
                ClientId = src.ClientId,
                Amount = src.Amount,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                Comment = src.Comment,
                Type = src.Type,
                PositionId = src.PositionId,
                LegalEntity = src.LegalEntity,
                AuditLog = src.AuditLog
            };
        }
    }
}