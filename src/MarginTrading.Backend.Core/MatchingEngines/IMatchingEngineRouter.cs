using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IMatchingEngineRouter
    {
        IMatchingEngineBase GetMatchingEngineForOpen(IPosition order);
        
        IMatchingEngineBase GetMatchingEngineForClose(IPosition order);
    }
}
