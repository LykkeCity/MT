namespace MarginTrading.Backend.Contracts.Orders
{
    public class OrderChangeRequest
    {
        public decimal Price { get; set; }
        
        public OriginatorTypeContract Originator { get; set; }
        
        public string AdditionalInfo { get; set; }
    }
}