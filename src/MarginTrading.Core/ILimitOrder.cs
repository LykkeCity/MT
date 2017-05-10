namespace MarginTrading.Core
{
    public interface ILimitOrder
    {
        string MarketMakerId { get; }
        double Price { get; }
    }

    public class LimitOrder : BaseOrder, ILimitOrder
    {
        public string MarketMakerId { get; set; }
        public double Price { get; set; }
    }
}
