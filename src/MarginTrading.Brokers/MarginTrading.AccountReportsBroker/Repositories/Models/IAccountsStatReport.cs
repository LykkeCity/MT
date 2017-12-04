using System;

namespace MarginTrading.AccountReportsBroker.Repositories.Models
{
    public interface IAccountsStatReport
    {
        string Id { get; }
        DateTime Date { get; }
        string BaseAssetId { get; }
        string AccountId { get; }
        string ClientId { get; }
        string TradingConditionId { get; }
        double Balance { get; }
        double WithdrawTransferLimit { get; }
        double MarginCall { get; }
        double StopOut { get; }
        double TotalCapital { get; }
        double FreeMargin { get; }
        double MarginAvailable { get; }
        double UsedMargin { get; }
        double MarginInit { get; }
        double PnL { get; }
        double OpenPositionsCount { get; }
        double MarginUsageLevel { get; }
        bool IsLive { get; }
    }
}
