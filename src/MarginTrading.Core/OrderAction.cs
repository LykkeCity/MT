namespace MarginTrading.Core
{
    public class OrderAction : IOrderAction
    {
        public string OrderId { get; set; }
        public string LykkeOrderId { get; set; }
        public string TraderLykkeId { get; set; }
        public OrderDirection CoreSide { get; set; }
        public string CoreSymbol { get; set; }
        public string TraderExternalSymbol { get; set; }
        public double? TakerRequestedPrice { get; set; }
        public TimeForceCondition TimeForceCondition { get; set; }
        public TraderAction TraderAction { get; set; }
        public double? ExecutionDuration { get; set; }
        public bool IsLive { get; set; }
    }
}
