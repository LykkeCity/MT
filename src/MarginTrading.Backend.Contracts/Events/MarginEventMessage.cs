using System;

namespace MarginTrading.Backend.Contracts.Events
{
    public class MarginEventMessage
    {
        public string EventId { get; set; }
        public DateTime EventTime { get; set; }
        public MarginEventTypeContract EventType { get; set; }

        public string AccountId { get; set; }
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public decimal Balance { get; set; }
        public decimal WithdrawTransferLimit { get; set; }

        public decimal MarginCall1Level { get; set; }
        public decimal MarginCall2Level { get; set; }
        public decimal StopOutLevel { get; set; }
        public decimal TotalCapital { get; set; }
        public decimal FreeMargin { get; set; }
        public decimal MarginAvailable { get; set; }
        public decimal UsedMargin { get; set; }
        public decimal MarginInit { get; set; }
        public decimal PnL { get; set; }
        public decimal OpenPositionsCount { get; set; }
        public decimal MarginUsageLevel { get; set; }
    }
}