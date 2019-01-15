namespace MarginTrading.Backend.Contracts.Activities
{
    public class OrderChangedMetadata
    {
        public OrderChangedProperty UpdatedProperty { get; set; }
        
        public string OldValue { get; set; }
    }
}