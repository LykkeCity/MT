using System.Collections.Generic;

namespace MarginTrading.Core
{
    public interface IMatchingEngineRepository
    {
        object GetMatchingEngineById(string id);
        IMatchingEngine GetDefaultMatchingEngine();
        void InitMatchingEngines(IEnumerable<object> matchingEngines);
    }
}
