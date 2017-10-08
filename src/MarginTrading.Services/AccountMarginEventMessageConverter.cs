using System;
using MarginTrading.Core;
using MarginTrading.Core.RabbitMqMessages;

namespace MarginTrading.Services
{
    class AccountMarginEventMessageConverter
    {
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
