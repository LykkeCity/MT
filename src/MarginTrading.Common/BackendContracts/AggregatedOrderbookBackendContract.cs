namespace MarginTrading.Common.BackendContracts
{
    public class AggregatedOrderbookBackendResponse
    {
        public AggregatedOrderBookItemBackendContract[] Buy { get; set; }
        public AggregatedOrderBookItemBackendContract[] Sell { get; set; }
    }

    public class AggregatedOrderBookItemBackendContract
    {
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
    }
}
