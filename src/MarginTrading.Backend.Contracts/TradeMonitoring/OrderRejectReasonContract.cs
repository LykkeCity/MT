// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.TradeMonitoring
{
    public enum OrderRejectReasonContract
    {
        None,
        NoLiquidity,
        NotEnoughBalance,
        LeadToStopOut,
        AccountInvalidState,
        InvalidExpectedOpenPrice,
        InvalidVolume,
        InvalidTakeProfit,
        InvalidStoploss,
        InvalidInstrument,
        InstrumentTradingDisabled,
        InvalidAccount,
        InvalidParent,
        TradingConditionError,
        InvalidValidity,
        TechnicalError,
        ParentPositionDoesNotExist,
        ParentPositionIsNotActive,
        ShortPositionsDisabled,
        MaxPositionLimit,
        MinOrderSizeLimit,
        MaxOrderSizeLimit,
        MaxPositionNotionalLimit,
    }
}
