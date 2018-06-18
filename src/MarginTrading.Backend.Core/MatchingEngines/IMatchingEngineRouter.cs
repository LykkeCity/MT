using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IMatchingEngineRouter
    {
        IMatchingEngineBase GetMatchingEngineForExecution(Order order);
        
        IMatchingEngineBase GetMatchingEngineForClose(Position order);
    }
}
