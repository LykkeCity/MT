namespace MarginTrading.Contract.RabbitMqMessageModels
{
    public class AccountStatsContract
    {
        public string AccountId { get; set; }
        public string BaseAssetId { get; set; }
        public string ClientId { get; set; }
        public string TradingConditionId { get; set; }
        public decimal Balance { get; set; }
        public decimal WithdrawTransferLimit { get; set; }
        public decimal MarginCallLevel { get; set; }
        public decimal StopOutLevel { get; set; }
        public decimal TotalCapital { get; set; }
        public decimal FreeMargin { get; set; }
        public decimal MarginAvailable { get; set; }
        public decimal UsedMargin { get; set; }
        public decimal MarginInit { get; set; }
        public decimal PnL { get; set; }
        public decimal OpenPositionsCount { get; set; }
        public decimal MarginUsageLevel { get; set; }
        public string LegalEntity { get; set; }
    }
}