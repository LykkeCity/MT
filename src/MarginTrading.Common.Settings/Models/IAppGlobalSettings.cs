namespace MarginTrading.Common.Settings.Models
{
    public interface IAppGlobalSettings
    {
        string DepositUrl { get; }
        bool DebugMode { get; }
        string DefaultIosAssetGroup { get; set; }
        string DefaultAssetGroupForOther { get; set; }
        bool IsOnReview { get; }
        double? MinVersionOnReview { get; }
        string ReviewIosGroup { get; }
        double IcoLkkSold { get; }
        bool IsOnMaintenance { get; }
        int LowCashOutTimeoutMins { get; }
        int LowCashOutLimit { get; }
        bool MarginTradingEnabled { get; }
    }
}