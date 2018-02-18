namespace MarginTrading.Backend.Core.TradingConditions
{
    public interface ITradingCondition
    {
        string Id { get; }
        string Name { get; }
        string MatchingEngineId { get; }
        bool IsDefault { get; }
    }
}