﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MarginTrading.Backend.Core.MatchingEngines;

namespace MarginTrading.Backend.Services.MatchingEngines
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
        
        public void SaveRoute(IMatchingEngineRoute route)
        {
            _lockSlim.EnterWriteLock();

            try
            {
                _routes[route.Id] = route;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }
        
        public void DeleteRoute(string id)
        {
            _lockSlim.EnterWriteLock();

            try
            {
                if (_routes.ContainsKey(id))
                    _routes.Remove(id);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        internal void InitCache(IEnumerable<IMatchingEngineRoute> routes)
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
