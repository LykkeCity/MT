namespace MarginTrading.Common.BackendContracts
{
    public class AggregatedOrderbookBackendResponse
    {
        public AggregatedOrderBookItemBackendContract[] Buy { get; set; }
        public AggregatedOrderBookItemBackendContract[] Sell { get; set; }
    }

    public class AggregatedOrderBookItemBackendContract
    {
        public double Price { get; set; }
        public double Volume { get; set; }
    }
}
