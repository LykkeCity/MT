using MarginTrading.Core;

namespace MarginTrading.Common.ClientContracts
{
    public class InitAccountsLiveDemoClientResponse
    {
        public MarginTradingAccountClientContract[] Live { get; set; }
        public MarginTradingAccountClientContract[] Demo { get; set; }
    }

    public class MarginTradingAccountClientContract
    {
        public string Id { get; set; }
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
        public int OpenPositionsCount { get; set; }
        public decimal MarginUsageLevel { get; set; }
        public bool IsLive { get; set; }
    }
}
