using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class MatchingEngineRoutesManager : IStartable
    {
        private readonly MatchingEngineRoutesCacheService _routesCacheService;
        private readonly IMatchingEngineRoutesRepository _repository;

        public MatchingEngineRoutesManager(
            MatchingEngineRoutesCacheService routesCacheService,
            IMatchingEngineRoutesRepository repository)
        {
            _routesCacheService = routesCacheService;
            _repository = repository;
        }

        public void Start()
        {
            UpdateRoutesCacheAsync().Wait();
        }

        public async Task UpdateRoutesCacheAsync()
        {
            var routes = new List<IMatchingEngineRoute>();

            routes.AddRange(await _repository.GetAllGlobalRoutesAsync());
            routes.AddRange(await _repository.GetAllLocalRoutesAsync());

            _routesCacheService.InitCache(routes);
        }

        public async Task AddOrReplaceGlobalRouteAsync(IMatchingEngineRoute route)
        {
            await _repository.AddOrReplaceGlobalRouteAsync(route);
            await UpdateRoutesCacheAsync();
        }

        public async Task AddOrReplaceLocalRouteAsync(IMatchingEngineRoute route)
        {
            await _repository.AddOrReplaceLocalRouteAsync(route);
            await UpdateRoutesCacheAsync();
        }

        public async Task DeleteGlobalRouteAsync(string routeId)
        {
            await _repository.DeleteGlobalRouteAsync(routeId);
            await UpdateRoutesCacheAsync();
        }

        public async Task DeleteLocalRouteAsync(string routeId)
        {
            await _repository.DeleteLocalRouteAsync(routeId);
            await UpdateRoutesCacheAsync();
        }
    }
}
