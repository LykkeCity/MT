namespace MarginTrading.Core
{
    public interface IOrderAction
    {
        string OrderId { get; set; }
        string LykkeOrderId { get; set; }
        string TraderLykkeId { get; set; }
        OrderDirection CoreSide { get; set; }
        string CoreSymbol { get; set; }
        string TraderExternalSymbol { get; set; }
        double? TakerRequestedPrice { get; set; }
        TimeForceCondition TimeForceCondition { get; set; }
        TraderAction TraderAction { get; set; }
        double? ExecutionDuration { get; set; }
        bool IsLive { get; set; }
    }

    public enum TimeForceCondition
    {
        None = 0,
        TakeProfit = 1,
        StopLoss = 2,
        StopOut = 3
    }

    public enum OrderType 
    {
        LimitOrder = 1,
        MarketOrder = 2
    }

    public enum TraderAction
    {
        Open,
        Close,
        Cancel
    }
}
