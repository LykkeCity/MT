namespace MarginTrading.Backend.Core
{
    public interface IAccountMarginFreezing
    {
        string OperationId { get; }
        string ClientId { get; }
        string AccountId { get; }
        decimal Amount { get; }
    }
}