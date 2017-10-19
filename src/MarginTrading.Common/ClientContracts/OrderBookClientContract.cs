namespace MarginTrading.Common.ClientContracts
{
    public class OrderBookClientContract
    {
        public OrderBookLevelClientContract[] Buy { get; set; }
        public OrderBookLevelClientContract[] Sell { get; set; }
    }

    public class OrderBookLevelClientContract
    {
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
    }
}
