using MarginTrading.Core;

namespace MarginTrading.Common.BackendContracts
{
    public class MarginTradingAccountBackendContract
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public decimal Balance { get; set; }
        public decimal WithdrawTransferLimit { get; set; }
        public decimal MarginCall { get; set; }
        public decimal StopOut { get; set; }
        public decimal TotalCapital { get; set; }
        public decimal FreeMargin { get; set; }
        public decimal MarginAvailable { get; set; }
        public decimal UsedMargin { get; set; }
        public decimal MarginInit { get; set; }
        public decimal PnL { get; set; }
        public decimal OpenPositionsCount { get; set; }
        public decimal MarginUsageLevel { get; set; }
        public bool IsLive { get; set; }
    }
}
