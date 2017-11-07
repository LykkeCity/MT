using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace MarginTrading.SqlMigration.Models
{
    internal class AccountTransactionsReportsEntity : TableEntity, IAccountTransactionsReports
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

        decimal IAccountTransactionsReports.Amount => Convert.ToDecimal(Amount);
        decimal IAccountTransactionsReports.Balance => Convert.ToDecimal(Balance);
        decimal IAccountTransactionsReports.WithdrawTransferLimit => Convert.ToDecimal(WithdrawTransferLimit);
    }
}
