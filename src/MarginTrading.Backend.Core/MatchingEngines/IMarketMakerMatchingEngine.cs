namespace MarginTrading.Backend.Core.MatchingEngines
{
    public interface IMarketMakerMatchingEngine : IMatchingEngineBase
    {
        void SetOrders(SetOrderModel model);
        bool PingLock();
    }
}