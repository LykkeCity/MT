namespace MarginTrading.AccountReportsBroker.Repositories.Models
{
    public interface IAccountsStatReport
    {
        string BaseAssetId { get; }
        string AccountId { get; }
        string ClientId { get; }
        string TradingConditionId { get; }
        decimal Balance { get; }
        decimal WithdrawTransferLimit { get; }
        decimal MarginCall { get; }
        decimal StopOut { get; }
        decimal TotalCapital { get; }
        decimal FreeMargin { get; }
        decimal MarginAvailable { get; }
        decimal UsedMargin { get; }
        decimal MarginInit { get; }
        decimal PnL { get; }
        decimal OpenPositionsCount { get; }
        decimal MarginUsageLevel { get; }
        bool IsLive { get; }
    }
}
