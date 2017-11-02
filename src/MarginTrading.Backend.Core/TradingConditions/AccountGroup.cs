namespace MarginTrading.Backend.Core.TradingConditions
{
    public class AccountGroup : IAccountGroup
    {
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public decimal MarginCall { get; set; }
        public decimal StopOut { get; set; }
        public decimal DepositTransferLimit { get; set; }

        public static AccountGroup Create(IAccountGroup src)
        {
            return new AccountGroup
            {
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                MarginCall = src.MarginCall,
                StopOut = src.StopOut,
                DepositTransferLimit = src.DepositTransferLimit
            };
        }
    }
}