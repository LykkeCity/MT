namespace MarginTrading.MarketMaker.Models
{
    public struct OrderbookPosition
    {
        public OrderbookPosition(decimal price, decimal volume)
        {
            Volume = volume;
            Price = price;
        }

        public decimal Price { get; }
        public decimal Volume { get; }
    }
}
