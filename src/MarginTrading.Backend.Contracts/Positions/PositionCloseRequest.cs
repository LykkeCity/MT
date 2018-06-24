using MarginTrading.Backend.Contracts.Orders;

namespace MarginTrading.Backend.Contracts.Positions
{
    public class PositionCloseRequest
    {
        public OriginatorTypeContract Originator { get; set; }

        public string Comment { get; set; }
        
        public string AdditionalInfo { get; set; }
    }
}