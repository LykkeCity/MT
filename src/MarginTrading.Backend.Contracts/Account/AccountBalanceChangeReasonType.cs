// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.Account
{
    public enum AccountBalanceChangeReasonType
    {
        Deposit = 1,
        Withdraw = 2,
        Reset = 4,
        Swap = 5,
        Manual = 6,
        UnrealizedDailyPnL = 7,
        RealizedPnL = 8,
        Commission = 9,
        Dividend = 10,
        OnBehalf = 11,
        Tax = 12,
        TemporaryCashAdjustment = 13,
        CompensationPayments = 14,
        Create = 15
    }
}