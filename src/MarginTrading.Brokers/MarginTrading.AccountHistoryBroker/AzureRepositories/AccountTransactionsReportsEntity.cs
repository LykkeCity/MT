using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AccountHistoryBroker.AzureRepositories
{
    internal class AccountTransactionsReportsEntity : TableEntity
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
    }
}