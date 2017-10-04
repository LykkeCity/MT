using System;
using MarginTrading.Core;

namespace MarginTrading.Common.RabbitMqMessages
{
    public class AccountMarginEventMessage
    {
        public string EventId { get; set; }
        public DateTime EventTime { get; set; }
        public bool IsEventStopout { get; set; }

        public string AccountId { get; set; }
        public string ClientId { get; set; }
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

        public static AccountMarginEventMessage Create(IMarginTradingAccount account, bool isStopout, DateTime eventTime)
        {
            return new AccountMarginEventMessage
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventTime = eventTime,
                IsEventStopout = isStopout,

                ClientId = account.ClientId,
                AccountId = account.Id,
                TradingConditionId = account.TradingConditionId,
                BaseAssetId = account.BaseAssetId,
                Balance = account.Balance,
                WithdrawTransferLimit = account.WithdrawTransferLimit,

                MarginCall = account.GetMarginCall(),
                StopOut = account.GetStopOut(),
                TotalCapital = account.GetTotalCapital(),
                FreeMargin = account.GetFreeMargin(),
                MarginAvailable = account.GetMarginAvailable(),
                UsedMargin = account.GetUsedMargin(),
                MarginInit = account.GetMarginInit(),
                PnL = account.GetPnl(),
                OpenPositionsCount = account.GetOpenPositionsCount(),
                MarginUsageLevel = account.GetMarginUsageLevel(),
            };
        }
    }
}
