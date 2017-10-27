namespace MarginTrading.Core.MatchingEngines
{
    public interface IMatchingEngineRouter
    {
        IMatchingEngineBase GetMatchingEngine(string clientId, string tradingConditionId, string instrument, OrderDirection orderType);
    }
}
