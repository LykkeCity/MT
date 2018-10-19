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
        TechnicalError,
        ParentPositionDoesNotExist,
        ParentPositionIsNotActive
    }
}