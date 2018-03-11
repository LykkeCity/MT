using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;

namespace MarginTrading.Backend.Services.MatchingEngines
{
    public class MatchingEngineInMemoryRepository : IMatchingEngineRepository
    {
        private readonly Dictionary<string, IMatchingEngineBase> _matchingEngines;
        
        public MatchingEngineInMemoryRepository(
            IMarketMakerMatchingEngine marketMakerMatchingEngine,
            IStpMatchingEngine stpMatchingEngine)
        {
            var mes = new IMatchingEngineBase[]
            {
                new RejectMatchingEngine(),
                marketMakerMatchingEngine,
                stpMatchingEngine
            };

            _matchingEngines = mes.ToDictionary(me => me.Id);
        }

        public IMatchingEngineBase GetMatchingEngineById(string id)
        {
            if (_matchingEngines.TryGetValue(id, out var me))
                return me;

            throw new NotSupportedException($"Matching Engine with ID [{id}] not found");
        }

        public ICollection<IMatchingEngineBase> GetMatchingEngines()
        {
            return _matchingEngines.Values;
        }
    }
}
