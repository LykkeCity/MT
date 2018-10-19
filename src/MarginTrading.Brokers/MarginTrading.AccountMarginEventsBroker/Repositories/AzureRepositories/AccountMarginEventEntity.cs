using System;
using MarginTrading.AccountMarginEventsBroker.Repositories.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AccountMarginEventsBroker.Repositories.AzureRepositories
{
    internal class AccountMarginEventEntity : TableEntity, IAccountMarginEvent
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

        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public decimal Balance { get; set; }
        public decimal WithdrawTransferLimit { get; set; }

        public decimal MarginCall { get; set; }
        public decimal StopOut { get; set; }
        public decimal TotalCapital { get; set; }
        public decimal FreeMargin { get; set; }
        public decimal MarginAvailable { get; set; }
        public decimal UsedMargin { get; set; }
        public decimal MarginInit { get; set; }
        public decimal PnL { get; set; }
        public decimal OpenPositionsCount { get; set; }
        public decimal MarginUsageLevel { get; set; }

        public string Id => EventId;

        public static AccountMarginEventEntity Create(IAccountMarginEvent src)
        {
            return new AccountMarginEventEntity
            {
                AccountId = src.AccountId,
                Balance = src.Balance,
                BaseAssetId = src.BaseAssetId,
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