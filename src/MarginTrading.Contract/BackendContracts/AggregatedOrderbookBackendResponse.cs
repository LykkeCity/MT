namespace MarginTrading.Contract.BackendContracts
{
    public class AggregatedOrderbookBackendResponse
    {
        public AggregatedOrderBookItemBackendContract[] Buy { get; set; }
        public AggregatedOrderBookItemBackendContract[] Sell { get; set; }
    }
}
