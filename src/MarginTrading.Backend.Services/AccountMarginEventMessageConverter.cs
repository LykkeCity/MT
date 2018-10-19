using System;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services
{
    class AccountMarginEventMessageConverter
    {
        public static MarginEventMessage Create(IMarginTradingAccount account, MarginEventTypeContract eventType,
            DateTime eventTime)
        {
            return new MarginEventMessage
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventTime = eventTime,
                EventType = eventType,

                AccountId = account.Id,
                TradingConditionId = account.TradingConditionId,
                BaseAssetId = account.BaseAssetId,
                Balance = account.Balance,
                WithdrawTransferLimit = account.WithdrawTransferLimit,

                MarginCall1Level = account.GetMarginCall1Level(),
                MarginCall2Level = account.GetMarginCall2Level(),
                StopOutLevel = account.GetStopOutLevel(),
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
