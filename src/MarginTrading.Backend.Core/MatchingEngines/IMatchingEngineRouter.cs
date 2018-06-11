using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IMatchingEngineRouter
    {
        IMatchingEngineBase GetMatchingEngineForOpen(Order order);
        
        IMatchingEngineBase GetMatchingEngineForClose(IPosition order);
    }
}
