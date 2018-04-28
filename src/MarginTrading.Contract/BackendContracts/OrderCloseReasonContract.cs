namespace MarginTrading.Contract.BackendContracts
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
        CanceledByBroker,
        ClosedByBroker
    }
}