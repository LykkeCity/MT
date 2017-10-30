using System.Collections.Generic;

namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IMatchingEngineRepository
    {
        IMatchingEngineBase GetMatchingEngineById(string id);
        IInternalMatchingEngine GetDefaultMatchingEngine();
        void InitMatchingEngines(IEnumerable<IMatchingEngineBase> matchingEngines);
    }
}
