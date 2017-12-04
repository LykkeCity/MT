using System;
using MarginTrading.AccountMarginEventsBroker.Repositories.Models;
using Microsoft.WindowsAzure.Storage.Table;

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

        public string Id => EventId;

        public static AccountMarginEventReportEntity Create(IAccountMarginEventReport src)
        {
            return new AccountMarginEventReportEntity
            {
                AccountId = src.AccountId,
                Balance = src.Balance,
                BaseAssetId = src.BaseAssetId,
                ClientId = src.ClientId,
                EventId = src.EventId,
                EventTime = src.EventTime,
                FreeMargin = src.FreeMargin,
                IsEventStopout = src.IsEventStopout,
                MarginAvailable = src.MarginAvailable,
                MarginCall = src.MarginCall,
                MarginInit = src.MarginInit,
                MarginUsageLevel = src.MarginUsageLevel,
                OpenPositionsCount = src.OpenPositionsCount,
                PnL = src.PnL,
                StopOut = src.StopOut,
                TotalCapital = src.TotalCapital,
                TradingConditionId = src.TradingConditionId,
                UsedMargin = src.UsedMargin,
                WithdrawTransferLimit = src.WithdrawTransferLimit
            };
        }
    }
}