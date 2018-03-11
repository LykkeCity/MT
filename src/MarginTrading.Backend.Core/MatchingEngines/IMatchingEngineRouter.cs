namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IMatchingEngineRouter
    {
        IMatchingEngineBase GetMatchingEngineForOpen(IOrder order);
        
        IMatchingEngineBase GetMatchingEngineForClose(IOrder order);
    }
}
