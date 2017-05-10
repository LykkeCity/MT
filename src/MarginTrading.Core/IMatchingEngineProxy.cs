using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMatchingEngineProxy : IMatchingEngineBase
    {
        //these functions never throw exceptions but return empty matchedorders. All exceptions are handled inside these functions
        Task<MatchedOrder[]> GetMatchedOrdersForOpenAsync(IOrder order);
        Task<MatchedOrder[]> GetMatchedOrdersForCloseAsync(IOrder order);
    }
}
