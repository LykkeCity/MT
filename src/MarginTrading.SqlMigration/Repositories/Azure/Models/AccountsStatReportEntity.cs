using MarginTrading.AccountReportsBroker.Repositories.Models;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace MarginTrading.SqlMigration.Repositories.Azure.Models
{
    class AccountsStatReportEntity: TableEntity, IAccountsStatReport
    {
        public string BaseAssetId
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public string AccountId
        {
            get => RowKey;
            set => RowKey = value;
        }

        public string ClientId { get; set; }
        public string TradingConditionId { get; set; }
        public double Balance { get; set; }
        public double WithdrawTransferLimit { get; set; }
        public double MarginCall { get; set; }
        public double StopOut { get; set; }
        public double TotalCapital { get; set; }
        public double FreeMargin { get; set; }
        public double MarginAvailable { get; set; }
        public double UsedMargin { get; set; }
        public double MarginInit { get; set; }
        public double PnL { get; set; }
        public double OpenPositionsCount { get; set; }
        public double MarginUsageLevel { get; set; }
        public bool IsLive { get; set; }

        public string Id => AccountId;
        public DateTime Date => Timestamp.DateTime;

        decimal IAccountsStatReport.Balance => Convert.ToDecimal(Balance);
        decimal IAccountsStatReport.WithdrawTransferLimit => Convert.ToDecimal(WithdrawTransferLimit);
        decimal IAccountsStatReport.MarginCall => Convert.ToDecimal(MarginCall);
        decimal IAccountsStatReport.StopOut => Convert.ToDecimal(StopOut);
        decimal IAccountsStatReport.TotalCapital => Convert.ToDecimal(TotalCapital);
        decimal IAccountsStatReport.FreeMargin => Convert.ToDecimal(FreeMargin);
        decimal IAccountsStatReport.MarginAvailable => Convert.ToDecimal(MarginAvailable);
        decimal IAccountsStatReport.UsedMargin => Convert.ToDecimal(UsedMargin);
        decimal IAccountsStatReport.MarginInit => Convert.ToDecimal(MarginInit);
        decimal IAccountsStatReport.PnL => Convert.ToDecimal(PnL);
        decimal IAccountsStatReport.OpenPositionsCount => Convert.ToDecimal(OpenPositionsCount);
        decimal IAccountsStatReport.MarginUsageLevel => Convert.ToDecimal(MarginUsageLevel);
    }
}
