namespace MarginTrading.Backend.Contracts.Orders
{
    public class OrderCancelRequest
    {
        public OriginatorTypeContract Originator { get; set; }

        public string Comment { get; set; }
        
        public string AdditionalInfo { get; set; }
    }
}