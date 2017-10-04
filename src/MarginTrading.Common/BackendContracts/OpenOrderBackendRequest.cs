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
        public decimal? ExpectedOpenPrice { get; set; }
        public decimal Volume { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        public OrderFillType FillType { get; set; }
    }
}
