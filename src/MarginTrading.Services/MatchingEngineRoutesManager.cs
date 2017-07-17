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
            routes.AddRange(await _repository.GetAllRoutesAsync());
            _routesCacheService.InitCache(routes);
        }

        public async Task AddOrReplaceRouteAsync(IMatchingEngineRoute route)
        {
            await _repository.AddOrReplaceRouteAsync(route);
            await UpdateRoutesCacheAsync();
        }        
                
        public async Task DeleteRouteAsync(string routeId)
        {
            await _repository.DeleteRouteAsync(routeId);
            await UpdateRoutesCacheAsync();
        }
    }
}
