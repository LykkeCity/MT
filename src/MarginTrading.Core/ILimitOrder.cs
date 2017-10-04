namespace MarginTrading.Core
{
    public interface ILimitOrder
    {
        string MarketMakerId { get; }
        decimal Price { get; }
    }

    public class LimitOrder : BaseOrder, ILimitOrder
    {
        public string MarketMakerId { get; set; }
        public decimal Price { get; set; }
    }
}
