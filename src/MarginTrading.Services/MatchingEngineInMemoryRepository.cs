using System.Collections.Generic;
using System.Linq;
using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class MatchingEngineInMemoryRepository : IMatchingEngineRepository
    {
        private IEnumerable<object> _matchingEngines;

        public object GetMatchingEngineById(string id)
        {
            return _matchingEngines.FirstOrDefault(item => (item as IMatchingEngineBase)?.Id == id);
        }

        public IMatchingEngine GetDefaultMatchingEngine()
        {
            return (IMatchingEngine)_matchingEngines.FirstOrDefault(item => (item as IMatchingEngineBase)?.Id == MatchingEngines.Lykke);
        }

        public void InitMatchingEngines(IEnumerable<object> matchingEngines)
        {
            _matchingEngines = matchingEngines;
        }
    }
}
