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
        public double Balance { get; set; }
        public bool IsCurrent { get; set; }
        public double MarginCall { get; set; }
        public double StopOut { get; set; }
        public double TotalCapital { get; set; }
        public double FreeMargin { get; set; }
        public double MarginAvailable { get; set; }
        public double UsedMargin { get; set; }
        public double MarginInit { get; set; }
        public double PnL { get; set; }
        public double OpenPositionsCount { get; set; }
        public double MarginUsageLevel { get; set; }
        public bool IsLive { get; set; }
    }
}
