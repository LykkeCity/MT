namespace MarginTrading.Core
{
    public interface IMatchingEngineRoutesCacheService
    {
        IMatchingEngineRoute GetMatchingEngineRoute(string clientId, string tradingConditionId, string instrument, OrderDirection orderType);
        IMatchingEngineRoute[] GetRoutes();
        IMatchingEngineRoute GetRoute(string id);
    }
}
