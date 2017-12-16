namespace MarginTrading.Backend.Core.Orderbooks
{
    public class AggregatedOrderInfo
    {
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public bool IsBuy { get; set; }
    }
}