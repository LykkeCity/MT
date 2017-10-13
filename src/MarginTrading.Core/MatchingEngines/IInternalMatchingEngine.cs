using System.Collections.Generic;

namespace MarginTrading.Core.MatchingEngines
{
    public interface IInternalMatchingEngine : IMatchingEngineBase
    {
        void SetOrders(SetOrderModel model);
        Dictionary<string, OrderBook> GetOrderBook(List<string> marketMakerIds);
        bool PingLock();
    }
}