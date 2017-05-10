using MarginTrading.Core;

namespace MarginTrading.Common.BackendContracts
{
    public class OpenOrderBackendRequest
    {
        public string ClientId { get; set; }
        public NewOrderBackendContract Order { get; set; }
    }

    public class NewOrderBackendContract
    {
        public string AccountId { get; set; }
        public string Instrument { get; set; }
        public double? ExpectedOpenPrice { get; set; }
        public double Volume { get; set; }
        public double? TakeProfit { get; set; }
        public double? StopLoss { get; set; }
        public OrderFillType FillType { get; set; }
    }
}
