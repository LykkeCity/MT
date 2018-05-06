using MarginTrading.Backend.Contracts.Orders;

namespace MarginTrading.Backend.Contracts.Positions
{
    public class PositionCloseRequest
    {
        public string PositionId { get; set; }

        public OriginatorTypeContract Originator { get; set; }

        public string Comment { get; set; }
    }
}