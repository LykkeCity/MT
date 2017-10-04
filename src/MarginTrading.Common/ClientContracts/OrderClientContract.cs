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
        public decimal? ExpectedOpenPrice { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public DateTime? OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public decimal Volume { get; set; }
        public decimal MatchedVolume { get; set; }
        public decimal MatchedCloseVolume { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal? Fpl { get; set; }
        public decimal OpenCommission { get; set; }
        public decimal CloseCommission { get; set; }
        public decimal SwapCommission { get; set; }
    }
}
