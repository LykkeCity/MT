namespace MarginTrading.Backend.Contracts.Orders
{
    public class OrderCancelRequest
    {
        public string OrderId { get; set; }

        public OriginatorTypeContract Originator { get; set; }

        public string Comment { get; set; }
    }
}