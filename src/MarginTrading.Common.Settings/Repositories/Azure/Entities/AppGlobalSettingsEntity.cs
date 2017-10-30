using MarginTrading.Common.Settings.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.Common.Settings.Repositories.Azure.Entities
{
    public class AppGlobalSettingsEntity : TableEntity, IAppGlobalSettings
    {
        public static string GeneratePartitionKey()
        {
            return "Setup";
        }

        public static string GenerateRowKey()
        {
            return "AppSettings";
        }


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


        public static AppGlobalSettingsEntity Create(IAppGlobalSettings appGlobalSettings)
        {
            return new AppGlobalSettingsEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(),
                DepositUrl = appGlobalSettings.DepositUrl,
                DebugMode = appGlobalSettings.DebugMode,
                DefaultIosAssetGroup = appGlobalSettings.DefaultIosAssetGroup,
                DefaultAssetGroupForOther = appGlobalSettings.DefaultAssetGroupForOther,
                MinVersionOnReview = appGlobalSettings.MinVersionOnReview,
                ReviewIosGroup = appGlobalSettings.ReviewIosGroup,
                IsOnReview = appGlobalSettings.IsOnReview,
                IcoLkkSold = appGlobalSettings.IcoLkkSold,
                IsOnMaintenance = appGlobalSettings.IsOnMaintenance,
                LowCashOutLimit = appGlobalSettings.LowCashOutLimit,
                LowCashOutTimeoutMins = appGlobalSettings.LowCashOutTimeoutMins,
                MarginTradingEnabled = appGlobalSettings.MarginTradingEnabled
            };
        }
    }
}