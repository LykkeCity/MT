using System;

namespace MarginTrading.Common.ClientContracts
{
    public class ClientOrdersLiveDemoClientResponse
    {
        public OrderClientContract[] Live { get; set; }
        public OrderClientContract[] Demo { get; set; }
    }

    public class OrderClientContract
    {
        public string Id { get; set; }
        public string AccountId { get; set; }
        public string Instrument { get; set; }
        public int Type { get; set; }
        public int Status { get; set; }
        public int CloseReason { get; set; }
        public int RejectReason { get; set; }
        public string RejectReasonText { get; set; }
        public double? ExpectedOpenPrice { get; set; }
        public double OpenPrice { get; set; }
        public double ClosePrice { get; set; }
        public DateTime? OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public double Volume { get; set; }
        public double MatchedVolume { get; set; }
        public double MatchedCloseVolume { get; set; }
        public double? TakeProfit { get; set; }
        public double? StopLoss { get; set; }
        public double? Fpl { get; set; }
        public double OpenCommission { get; set; }
        public double CloseCommission { get; set; }
        public double SwapCommission { get; set; }
    }
}
