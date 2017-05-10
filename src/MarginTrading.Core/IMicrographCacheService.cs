using System.Collections.Generic;

namespace MarginTrading.Core
{
    public interface IMicrographCacheService
    {
        Dictionary<string, List<GraphBidAskPair>> GetGraphData();
    }
}
