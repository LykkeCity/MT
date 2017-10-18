using System;
using Microsoft.WindowsAzure.Storage.Table;
using MarginTrading.Core;

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

        public static AccountTransactionsReportsEntity Create(IAccountTransactionsReport src)
        {
            return new AccountTransactionsReportsEntity
            {
                Id = src.Id,
                Date = src.Date,
                ClientId = src.ClientId,
                Amount = (double)src.Amount,
                Balance = (double)src.Balance,
                WithdrawTransferLimit = (double)src.WithdrawTransferLimit,
                Comment = src.Comment,
                Type = src.Type.ToString()
            };
        }
    }
}