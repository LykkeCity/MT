namespace MarginTrading.Contract.ClientContracts
{
    public class AggregatedOrderbookLiveDemoClientContract
    {
        public AggregatedOrderbookClientContract Live { get; set; }
        public AggregatedOrderbookClientContract Demo { get; set; }
    }
}