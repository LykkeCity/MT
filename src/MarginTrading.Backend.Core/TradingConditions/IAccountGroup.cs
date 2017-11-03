namespace MarginTrading.Backend.Core.TradingConditions
{
    public interface IAccountGroup
    {
        string TradingConditionId { get; }
        string BaseAssetId { get; }
        decimal MarginCall { get; }
        decimal StopOut { get; }
        decimal DepositTransferLimit { get; }
        decimal ProfitWithdrawalLimit { get; }
    }
}