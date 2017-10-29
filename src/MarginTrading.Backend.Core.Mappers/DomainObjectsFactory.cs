using System;
using MarginTrading.Contract.BackendContracts;
using MarginTrading.Core;
using MarginTrading.Core.MatchingEngines;

namespace MarginTrading.Backend.Core.Mappers
{
    public class DomainObjectsFactory
    {
        public static IMatchingEngineRoute CreateRoute(NewMatchingEngineRouteRequest request, string id = null)
        {
            return new MatchingEngineRoute
            {
                Id = id ?? Guid.NewGuid().ToString().ToUpper(),
                Rank = request.Rank,
                TradingConditionId = request.TradingConditionId,
                ClientId = request.ClientId,
                Instrument = request.Instrument,
                Type = request.Type.ToType<OrderDirection>(),
                MatchingEngineId = request.MatchingEngineId,
                Asset = request.Asset
            };
        }
    }
}