namespace MarginTrading.Backend.Core.FakeExchangeConnector.Domain.Trading
{
    public enum OrderStatusUpdateFailureType
    {
        None,
        Unknown,
        ExchangeError,
        ConnectorError,
        InsufficientFunds
    }
}
