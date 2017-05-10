namespace MarginTrading.Common.ClientContracts
{
    public class AggregatedOrderbookLiveDemoClientContract
    {
        public AggregatedOrderbookClientContract Live { get; set; }
        public AggregatedOrderbookClientContract Demo { get; set; }
    }

    public class AggregatedOrderbookClientContract
    {
        public AggregatedOrderBookItemClientContract[] Buy { get; set; }
        public AggregatedOrderBookItemClientContract[] Sell { get; set; }
    }

    public class AggregatedOrderBookItemClientContract
    {
        public double Price { get; set; }
        public double Volume { get; set; }
    }
}
