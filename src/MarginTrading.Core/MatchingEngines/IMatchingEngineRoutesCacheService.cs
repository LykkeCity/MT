namespace MarginTrading.Core.MatchingEngines
{
    public interface IMatchingEngineRoutesCacheService
    {
        IMatchingEngineRoute[] GetRoutes();
        IMatchingEngineRoute GetRoute(string id);
        void SaveRoute(IMatchingEngineRoute route);
        void DeleteRoute(string id);
    }
}
