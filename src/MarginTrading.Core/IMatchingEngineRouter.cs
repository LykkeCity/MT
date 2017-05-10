namespace MarginTrading.Core
{
    public interface IMatchingEngineRouter
    {
        object GetMatchingEngine(string clientId, string tradingConditionId, string instrument, OrderDirection orderType);
    }
}
