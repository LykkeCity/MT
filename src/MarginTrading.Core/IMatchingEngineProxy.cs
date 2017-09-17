using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMatchingEngineProxy : IMatchingEngineBase
    {
        //these functions never throw exceptions but return empty matchedorders. All exceptions are handled inside these functions
        Task<MatchedOrderCollection> GetMatchedOrdersForOpenAsync(IOrder order);
        Task<MatchedOrderCollection> GetMatchedOrdersForCloseAsync(IOrder order);
    }
}
