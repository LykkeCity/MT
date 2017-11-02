namespace MarginTrading.Backend.Core.TradingConditions
{
    public interface ITradingCondition
    {
        string Id { get; }
        string Name { get; }
        bool IsDefault { get; }
    }
}