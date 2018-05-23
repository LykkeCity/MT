namespace MarginTrading.Backend.Core.FakeExchangeConnector.Domain.Trading
{
    public enum AcknowledgementFailureType
    {
        None,
        Unknown,
        ExchangeError,
        ConnectorError,
        InsufficientFunds
    }
}
