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
