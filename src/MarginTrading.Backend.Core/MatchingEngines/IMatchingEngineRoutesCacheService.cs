// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IMatchingEngineRoutesCacheService
    {
        IMatchingEngineRoute[] GetRoutes();
        IMatchingEngineRoute GetRoute(string id);
        void SaveRoute(IMatchingEngineRoute route);
        void DeleteRoute(string id);
    }
}
