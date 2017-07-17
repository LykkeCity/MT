using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class MatchingEngineRoutesCacheService : IMatchingEngineRoutesCacheService
    {
        private List<IMatchingEngineRoute> _routes = new List<IMatchingEngineRoute>();

        public IMatchingEngineRoute GetMatchingEngineRoute(string clientId, string tradingConditionId, string instrument, OrderDirection orderType)
        {
            var localRoute = _routes
                .OrderBy(item => item.Rank)
                .FirstOrDefault(item => item.ClientId == clientId 
                    && (item.Instrument == instrument || string.IsNullOrEmpty(item.Instrument))
                    && (item.Type == orderType || item.Type == null));

            if (localRoute != null)
                return localRoute;

            return _routes.FirstOrDefault(item => item.TradingConditionId == tradingConditionId
                    && (item.Instrument == instrument || string.IsNullOrEmpty(item.Instrument))
                    && (item.Type == orderType || item.Type == null));
        }

        public IMatchingEngineRoute GetMatchingEngineRouteById(string id)
        {
            return _routes.FirstOrDefault(item => item.Id == id);
        }

        public IMatchingEngineRoute GetRoute(string id)
        {
            return _routes.Where(item => item.Id == id)
                .OrderBy(item => item.Rank)
                .FirstOrDefault();
        }

        public IMatchingEngineRoute[] GetRoutes()
        {
            return _routes.OrderBy(item => item.Rank)
                .ToArray();
        }

        //public IMatchingEngineRoute[] GetGlobalRoutes()
        //{
        //    return _routes.Where(item => string.IsNullOrEmpty(item.ClientId))
        //        .OrderBy(item => item.Rank)
        //        .ToArray();
        //}

        //public IMatchingEngineRoute[] GetLocalRoutes()
        //{
        //    return _routes.Where(item => string.IsNullOrEmpty(item.TradingConditionId))
        //        .OrderBy(item => item.Rank)
        //        .ToArray();
        //}

        internal void InitCache(List<IMatchingEngineRoute> routes)
        {
            _routes = routes;
        }
    }
}
