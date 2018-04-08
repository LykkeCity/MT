using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Contract.BackendContracts
{
    public class OrderBackendContract
    {
        public string Id { get; set; }
        public string AccountId { get; set; }
        public string Instrument { get; set; }
        public OrderDirectionContract Type { get; set; }
        public OrderStatusContract Status { get; set; }
        public OrderCloseReasonContract CloseReason { get; set; }
        public OrderRejectReasonContract RejectReason { get; set; }
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
        public decimal CommissionLot { get; set; }
        public decimal OpenCommission { get; set; }
        public decimal CloseCommission { get; set; }
        public decimal SwapCommission { get; set; }
        public string EquivalentAsset { get; set; }
        public decimal OpenPriceEquivalent{ get; set; }
        public decimal ClosePriceEquivalent { get; set; }
        public string OpenExternalOrderId { get; set; }
        public string OpenExternalProviderId { get; set; }
        public string CloseExternalOrderId { get; set; }
        public string CloseExternalProviderId { get; set; }
        public string LegalEntity { get; set; }  

        [JsonConverter(typeof(StringEnumConverter))]
        public MatchingEngineModeContract MatchingEngineMode { get; set; }

    }
}
