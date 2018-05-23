namespace MarginTrading.Backend.Core.FakeExchangeConnector.Domain
{
    public enum ExchangeState
    {
        Initializing,
        Connecting,
        ReconnectingAfterError,
        Connected,
        ReceivingPrices,
        ExecuteOrders,
        ErrorState,
        Stopped,
        Stopping
    }
}