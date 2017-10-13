using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Core;
using MarginTrading.Core.MatchingEngines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/routes")]
    public class RoutesController : Controller
    {
        private readonly IMatchingEngineRoutesRepository _routesRepository;

        public RoutesController(IMatchingEngineRoutesRepository routesRepository)
        {
            _routesRepository = routesRepository;
        }

        /// <summary>
        /// Gets all routes
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<MatchingEngineRoute>> GetAllRoutes()
        {
            return (await _routesRepository.GetAllRoutesAsync()).Select(TransformRoute);
        }

        /// <summary>
        /// Gets a route by <paramref name="id"/>
        /// </summary>
        [HttpGet]
        [Route("{id}")]
        public async Task<MatchingEngineRoute> GetRouteById(string id)
        {
            return TransformRoute(await _routesRepository.GetRouteByIdAsync(id));
        }

        private static MatchingEngineRoute TransformRoute(IMatchingEngineRoute route)
        {
            string GetEmptyIfAny(string value)
            {
                const string AnyValue = "*";
                return value == AnyValue ? null : value;
            }

            return new MatchingEngineRoute
            {
                Id = route.Id,
                Rank = route.Rank,
                MatchingEngineId = route.MatchingEngineId,
                Asset = GetEmptyIfAny(route.Asset),
                ClientId = GetEmptyIfAny(route.ClientId),
                Instrument = GetEmptyIfAny(route.Instrument),
                TradingConditionId = GetEmptyIfAny(route.TradingConditionId),
                Type = route.Type
            };
        }
    }
}