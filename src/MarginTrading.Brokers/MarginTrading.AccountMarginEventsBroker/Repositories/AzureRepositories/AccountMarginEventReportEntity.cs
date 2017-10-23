using System;
using Microsoft.WindowsAzure.Storage.Table;
using MarginTrading.Core;

namespace MarginTrading.AccountMarginEventsBroker.Repositories.AzureRepositories
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

        public static AccountMarginEventReportEntity Create(IAccountMarginEventReport src)
        {
            return new AccountMarginEventReportEntity
            {
                AccountId = src.AccountId,
                Balance = (double)src.Balance,
                BaseAssetId = src.BaseAssetId,
                ClientId = src.ClientId,
                EventId = src.EventId,
                EventTime = src.EventTime,
                FreeMargin = (double)src.FreeMargin,
                IsEventStopout = src.IsEventStopout,
                MarginAvailable = (double)src.MarginAvailable,
                MarginCall = (double)src.MarginCall,
                MarginInit = (double)src.MarginInit,
                MarginUsageLevel = (double)src.MarginUsageLevel,
                OpenPositionsCount = (double)src.OpenPositionsCount,
                PnL = (double)src.PnL,
                StopOut = (double)src.StopOut,
                TotalCapital = (double)src.TotalCapital,
                TradingConditionId = src.TradingConditionId,
                UsedMargin = (double)src.UsedMargin,
                WithdrawTransferLimit = (double)src.WithdrawTransferLimit
            };
        }
    }
}