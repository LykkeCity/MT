using System.Collections.Generic;

namespace MarginTrading.Backend.Core
{
    public interface IMicrographCacheService
    {
        Dictionary<string, List<GraphBidAskPair>> GetGraphData();
    }
}
