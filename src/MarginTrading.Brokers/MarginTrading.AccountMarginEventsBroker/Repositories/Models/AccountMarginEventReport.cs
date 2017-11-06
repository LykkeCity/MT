﻿using System;

namespace MarginTrading.AccountMarginEventsBroker.Repositories.Models
{
    internal class AccountMarginEventReport : IAccountMarginEventReport
    {
        public string Id => EventId;
        public string AccountId { get; set; }
        public decimal Balance { get; set; }
        public string BaseAssetId { get; set; }
        public string ClientId { get; set; }
        public string EventId { get; set; }
        public DateTime EventTime { get; set; }
        public decimal FreeMargin { get; set; }
        public bool IsEventStopout { get; set; }
        public decimal MarginAvailable { get; set; }
        public decimal MarginCall { get; set; }
        public decimal MarginInit { get; set; }
        public decimal MarginUsageLevel { get; set; }
        public decimal OpenPositionsCount { get; set; }
        public decimal PnL { get; set; }
        public decimal StopOut { get; set; }
        public decimal TotalCapital { get; set; }
        public string TradingConditionId { get; set; }
        public decimal UsedMargin { get; set; }
        public decimal WithdrawTransferLimit { get; set; }
    }
}
