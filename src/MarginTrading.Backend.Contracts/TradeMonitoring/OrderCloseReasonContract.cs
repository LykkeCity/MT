namespace MarginTrading.Backend.Contracts.TradeMonitoring
{
    public enum OrderCloseReasonContract
    {
        None,
        Close,
        StopLoss,
        TakeProfit,
        StopOut,
        Canceled,
        CanceledBySystem,
        ClosedByBroker
    }
}
