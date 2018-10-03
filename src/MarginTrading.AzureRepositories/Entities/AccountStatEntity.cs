using System;
using Lykke.AzureStorage.Tables;
using MarginTrading.Backend.Core;

namespace MarginTrading.AzureRepositories.Entities
{
    public class AccountStatEntity : AzureTableEntity
    {
        public string Id => RowKey;
        
        public decimal PnL { get; set; }
        public decimal UnrealizedDailyPnl { get; set; }
        public decimal UsedMargin { get; set; }
        public decimal MarginInit { get; set; }
        public int OpenPositionsCount { get; set; }
        public decimal MarginCall1Level { get; set; }
        public decimal MarginCall2Level { get; set; }
        public decimal StopoutLevel { get; set; }

        public decimal WithdrawalFrozenMargin { get; set; }
        public decimal UnconfirmedMargin { get; set; }
        
        public DateTime HistoryTimestamp { get; set; }
        
        public static AccountStatEntity Create(MarginTradingAccount account, DateTime now)
        {
            return new AccountStatEntity
            {
                PartitionKey = "AccountStats",
                RowKey = account.Id,
                PnL = account.GetPnl(),
                UnrealizedDailyPnl = account.GetUnrealizedDailyPnl(),
                UsedMargin = account.GetUsedMargin(),
                MarginInit = account.GetMarginInit(),
                OpenPositionsCount = account.GetOpenPositionsCount(),
                MarginCall1Level = account.GetMarginCall1Level(),
                MarginCall2Level = account.GetMarginCall2Level(),
                StopoutLevel = account.GetStopOutLevel(),
                WithdrawalFrozenMargin = account.GetFrozenMargin(),
                UnconfirmedMargin = account.GetUnconfirmedMargin(),
                HistoryTimestamp = now,
            };
        }
    }
}