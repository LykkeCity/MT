namespace MarginTrading.Backend.Contracts.TradeMonitoring
{
    public class LimitOrderContract : BaseOrderContract
    {
        public string MarketMakerId { get; set; }
        public decimal Price { get; set; }
    }
}
