namespace MarginTrading.Common.Settings.Models
{
    public class AppGlobalSettings : IAppGlobalSettings
    {
        public string DepositUrl { get; set; }
        public bool DebugMode { get; set; }
        public string DefaultIosAssetGroup { get; set; }
        public string DefaultAssetGroupForOther { get; set; }
        public bool IsOnReview { get; set; }
        public double? MinVersionOnReview { get; set; }
        public string ReviewIosGroup { get; set; }
        public double IcoLkkSold { get; set; }
        public bool IsOnMaintenance { get; set; }
        public int LowCashOutTimeoutMins { get; set; }
        public int LowCashOutLimit { get; set; }
        public bool MarginTradingEnabled { get; set; }

        public static AppGlobalSettings CreateDefault()
        {
            return new AppGlobalSettings
            {
                DepositUrl = "http://mock-bankcards.azurewebsites.net/",
                DebugMode = true
            };
        }
    }
}
