using System;

namespace MarginTrading.AccountMarginEventsBroker.Repositories.Models
{
    internal interface IAccountMarginEventReport
    {
        string Id { get; }
        string AccountId { get; }
        double Balance { get; }
        string BaseAssetId { get; }
        string ClientId { get; }
        string EventId { get; }
        DateTime EventTime { get; }
        double FreeMargin { get; }
        bool IsEventStopout { get; }
        double MarginAvailable { get; }
        double MarginCall { get; }
        double MarginInit { get; }
        double MarginUsageLevel { get; }
        double OpenPositionsCount { get; }
        double PnL { get; }
        double StopOut { get; }
        double TotalCapital { get; }
        string TradingConditionId { get; }
        double UsedMargin { get; }
        double WithdrawTransferLimit { get; }
    }
}
