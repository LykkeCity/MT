namespace MarginTrading.Backend.Core.Orders
{
    public abstract class RelatedOrderInfo
    {
        public OrderType Type { get; set; }
        public string Id { get; set; }
    }
}