// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Contracts.Account
{
    public class AccountCapitalFigures
    {
        public decimal Balance { get; set; }
        public DateTime LastBalanceChangeTime { get; set; }
        public decimal TotalCapital { get; set; }
        public decimal FreeMargin { get; set; }
        public decimal UsedMargin { get; set; }
        public decimal CurrentlyUsedMargin { get; set; }
        public decimal InitiallyUsedMargin { get; set; }
        public decimal PnL { get; set; }
        public decimal UnrealizedDailyPnl { get; set; }
        public int OpenPositionsCount { get; set; }
        public string AdditionalInfo { get; set; }

        public decimal TodayRealizedPnL { get; set; }
        public decimal TodayUnrealizedPnL { get; set; }
        public decimal TodayDepositAmount { get; set; }
        public decimal TodayWithdrawAmount { get; set; }
        public decimal TodayCommissionAmount { get; set; }
        public decimal TodayOtherAmount { get; set; }
        public decimal TodayStartBalance { get; set; }
        public bool AccountIsDeleted { get; set; }
        public decimal UnconfirmedMargin { get; set; }

        public static AccountCapitalFigures Empty = new AccountCapitalFigures()
        {
            AdditionalInfo = "{}",
        };
        
        public static AccountCapitalFigures Deleted = new AccountCapitalFigures()
        {
            AdditionalInfo = "{}",
            AccountIsDeleted = true,
        };
    }
}