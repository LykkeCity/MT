namespace MarginTrading.Contract.BackendContracts.AccountsManagement
{
    public class AccountsMarginLevelContract
    {
        public string AccountId { get; set; }
        
        public string ClientId { get; set; }

        public string TradingConditionId { get; set; }
        
        public string BaseAssetId { get; set; }
        
        public decimal Balance { get; set; }
        
        /// <summary>
        /// Used margin / (balance + pnl)
        /// </summary>
        public decimal MarginLevel { get; set; }
        
        public int OpenedPositionsCount { get; set; }
    }
}