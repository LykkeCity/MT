namespace MarginTrading.Core
{
    public interface IMatchingEngineRoutesCacheService
    {
        IMatchingEngineRoute[] GetRoutes();
        IMatchingEngineRoute GetRoute(string id);
    }
}
