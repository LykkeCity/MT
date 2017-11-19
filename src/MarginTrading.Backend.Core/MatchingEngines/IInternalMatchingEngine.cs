using MarginTrading.Backend.Core.Orderbooks;

namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IInternalMatchingEngine : IMatchingEngineBase
    {
        void SetOrders(SetOrderModel model);
        OrderBook GetOrderBook(string instrument);
        bool PingLock();
    }
}