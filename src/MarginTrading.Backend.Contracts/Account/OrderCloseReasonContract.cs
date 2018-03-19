namespace MarginTrading.Backend.Contracts.Account
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