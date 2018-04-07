using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Routes;
using MarginTrading.Backend.Contracts.TradeMonitoring;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/routes")]
    public class RoutesController : Controller, IRoutesReadingApi
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
        public async Task<List<MatchingEngineRouteContract>> List()
        {
            return (await _routesRepository.GetAllRoutesAsync()).Select(TransformRoute).ToList();
        }

        /// <summary>
        /// Gets a route by <paramref name="id"/>
        /// </summary>
        [HttpGet]
        [Route("{id}")]
        public async Task<MatchingEngineRouteContract> GetById(string id)
        {
            return TransformRoute(await _routesRepository.GetRouteByIdAsync(id));
        }

        private static MatchingEngineRouteContract TransformRoute(IMatchingEngineRoute route)
        {
            string GetEmptyIfAny(string value)
            {
                const string AnyValue = "*";
                return value == AnyValue ? null : value;
            }
            OrderDirectionContract? GetDirection(OrderDirection? direction)
            {
                switch (direction)
                {
                    case null:
                        return null;
                    case OrderDirection.Buy:
                        return OrderDirectionContract.Buy;
                    case OrderDirection.Sell:
                        return OrderDirectionContract.Sell;
                    default:
                        throw new System.ArgumentException($"Invalid Value: {direction?.ToString()}", nameof(direction));
                }
            }

            return new MatchingEngineRouteContract
            {
                Id = route.Id,
                Rank = route.Rank,
                MatchingEngineId = route.MatchingEngineId,
                Asset = GetEmptyIfAny(route.Asset),
                ClientId = GetEmptyIfAny(route.ClientId),
                Instrument = GetEmptyIfAny(route.Instrument),
                TradingConditionId = GetEmptyIfAny(route.TradingConditionId),
                Type = GetDirection(route.Type)
            };
        }
    }
}