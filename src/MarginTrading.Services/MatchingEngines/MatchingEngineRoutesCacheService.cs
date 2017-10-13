using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MarginTrading.Core.MatchingEngines;

namespace MarginTrading.Services.MatchingEngines
{
    public class MatchingEngineRoutesCacheService : IMatchingEngineRoutesCacheService
    {
        private Dictionary<string, IMatchingEngineRoute> _routes = new Dictionary<string, IMatchingEngineRoute>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public IMatchingEngineRoute GetRoute(string id)
        {
            _lockSlim.EnterReadLock();

            try
            {
                return _routes[id];
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public IMatchingEngineRoute[] GetRoutes()
        {
            _lockSlim.EnterReadLock();

            try
            {
                return _routes.Values.ToArray();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        internal void InitCache(List<IMatchingEngineRoute> routes)
        {
            _lockSlim.EnterWriteLock();

            try
            {
                _routes = routes.OrderBy(r => r.Rank).ToDictionary(r => r.Id);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }
    }
}
