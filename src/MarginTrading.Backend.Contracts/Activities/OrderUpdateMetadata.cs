namespace MarginTrading.Backend.Contracts.Activities
{
    public class OrderUpdateMetadata
    {
        public OrderUpdatedProperty UpdatedProperty { get; set; }
        
        public string OldValue { get; set; }
    }
}