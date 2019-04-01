namespace MarginTrading.Backend.Core.Orders
{
    public enum OrderRejectReason
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
    }
}