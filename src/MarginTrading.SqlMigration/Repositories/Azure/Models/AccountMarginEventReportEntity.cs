using MarginTrading.AccountMarginEventsBroker.Repositories.Models;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace MarginTrading.SqlMigration.Repositories.Azure.Models
{
    internal class AccountMarginEventReportEntity : TableEntity, IAccountMarginEventReport
    {
        public string EventId
        {
            get => RowKey;
            set => RowKey = value;
        }

        public string AccountId
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public DateTime EventTime { get; set; }
        public bool IsEventStopout { get; set; }

        public string ClientId { get; set; }
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
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

        public string Id => EventId;

        decimal IAccountMarginEventReport.Balance => Convert.ToDecimal(Balance);
        decimal IAccountMarginEventReport.FreeMargin => Convert.ToDecimal(FreeMargin);
        decimal IAccountMarginEventReport.MarginAvailable => Convert.ToDecimal(MarginAvailable);
        decimal IAccountMarginEventReport.MarginCall => Convert.ToDecimal(MarginCall);
        decimal IAccountMarginEventReport.MarginInit => Convert.ToDecimal(MarginInit);
        decimal IAccountMarginEventReport.MarginUsageLevel => Convert.ToDecimal(MarginUsageLevel);
        decimal IAccountMarginEventReport.OpenPositionsCount => Convert.ToDecimal(OpenPositionsCount);
        decimal IAccountMarginEventReport.PnL => Convert.ToDecimal(PnL);
        decimal IAccountMarginEventReport.StopOut => Convert.ToDecimal(StopOut);
        decimal IAccountMarginEventReport.TotalCapital => Convert.ToDecimal(TotalCapital);
        decimal IAccountMarginEventReport.UsedMargin => Convert.ToDecimal(UsedMargin);
        decimal IAccountMarginEventReport.WithdrawTransferLimit => Convert.ToDecimal(WithdrawTransferLimit);
        
    }
}
