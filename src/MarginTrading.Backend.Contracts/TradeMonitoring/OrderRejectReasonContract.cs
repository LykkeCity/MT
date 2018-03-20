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
        InvalidAccount,
        TradingConditionError,
        TechnicalError
    }
}
