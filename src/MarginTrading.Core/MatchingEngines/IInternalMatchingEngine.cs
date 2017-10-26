namespace MarginTrading.Core.MatchingEngines
{
    public interface IInternalMatchingEngine : IMatchingEngineBase
    {
        void SetOrders(SetOrderModel model);
        OrderBook GetOrderBook(string instrument);
        bool PingLock();
    }
}