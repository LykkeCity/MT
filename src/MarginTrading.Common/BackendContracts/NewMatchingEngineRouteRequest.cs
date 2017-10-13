using System;
using MarginTrading.Core;
using MarginTrading.Core.MatchingEngines;

namespace MarginTrading.Common.BackendContracts
{
    public class NewMatchingEngineRouteRequest 
    {        
        public int Rank { get; set; }
        public string TradingConditionId { get; set; }
        public string ClientId { get; set; }
        public string Instrument { get; set; }
        public OrderDirection? Type { get; set; }
        public string MatchingEngineId { get; set; }
        public string Asset { get; set; }

        public static IMatchingEngineRoute CreateRoute(NewMatchingEngineRouteRequest request, string id = null)
        {
            return new MatchingEngineRoute
            {
                Id = id ?? Guid.NewGuid().ToString().ToUpper(),
                Rank = request.Rank,
                TradingConditionId = request.TradingConditionId,
                ClientId = request.ClientId,
                Instrument = request.Instrument,
                Type = request.Type,
                MatchingEngineId = request.MatchingEngineId,
                Asset = request.Asset
            };
        }

    }
}
