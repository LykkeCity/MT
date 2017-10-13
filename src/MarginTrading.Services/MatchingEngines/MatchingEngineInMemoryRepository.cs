using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Core;
using MarginTrading.Core.MatchingEngines;

namespace MarginTrading.Services.MatchingEngines
{
    public class MatchingEngineInMemoryRepository : IMatchingEngineRepository
    {
        private Dictionary<string, IMatchingEngineBase> _matchingEngines;

        public IMatchingEngineBase GetMatchingEngineById(string id)
        {
            if (_matchingEngines.TryGetValue(id, out var me))
                return me;

            throw new NotSupportedException($"Matching Engine with ID [{id}] not found");
        }

        public IInternalMatchingEngine GetDefaultMatchingEngine()
        {
            return (IInternalMatchingEngine) GetMatchingEngineById(MatchingEngineConstants.Lykke);
        }

        public void InitMatchingEngines(IEnumerable<IMatchingEngineBase> matchingEngines)
        {
            _matchingEngines = matchingEngines.ToDictionary(me => me.Id);
        }
    }
}
