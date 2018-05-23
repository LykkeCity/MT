namespace MarginTrading.Backend.Core.FakeExchangeConnector.Domain.Trading
{
    public enum OrderExecutionStatus
    {
        Unknown,
        Fill,
        PartialFill,
        Cancelled,
        Rejected,
        New,
        Pending
    }
}
