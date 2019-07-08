// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.AccountMarginEventsBroker.Repositories.Models;
using MarginTrading.Backend.Contracts.Events;

namespace MarginTrading.AccountMarginEventsBroker.Repositories.SqlRepositories
{
    internal class AccountMarginEventEntity : IAccountMarginEvent
    {
        public string Id { get; set; }
        public string AccountId { get; set; }
        public decimal Balance { get; set; }
        public string BaseAssetId { get; set; }
        public string EventId { get; set; }
        public DateTime EventTime { get; set; }
        public decimal FreeMargin { get; set; }
        public bool IsEventStopout { get; set; }
        public string EventType { get; set; }
        MarginEventTypeContract IAccountMarginEvent.EventType => Enum.Parse<MarginEventTypeContract>(EventType);
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

        public static AccountMarginEventEntity Create(IAccountMarginEvent src)
        {
            return new AccountMarginEventEntity
            {
                Id = src.Id,
                AccountId = src.AccountId,
                Balance = src.Balance,
                BaseAssetId = src.BaseAssetId,
                EventId = src.EventId,
                EventTime = src.EventTime,
                FreeMargin = src.FreeMargin,
                IsEventStopout = src.IsEventStopout,
                EventType = src.EventType.ToString(),
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