namespace MarginTrading.Backend.Contracts.TradingConditions
{
    public class AccountGroupContract
    {
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public decimal MarginCall { get; set; }
        public decimal StopOut { get; set; }
        public decimal DepositTransferLimit { get; set; }
        public decimal ProfitWithdrawalLimit { get; set; }
    }
}
