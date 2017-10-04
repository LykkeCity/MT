using System.Threading.Tasks;

namespace MarginTrading.Core.Settings
{
    public static class GlobalSettings
    {
        public const int Mt4TimeOffset = 2;
        public const string AssetAccuracyMask = "0.##########";



        public static string ToAssetMultiplierString(this decimal src)
        {
            return src.ToString(AssetAccuracyMask);
        }
    }


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

    public interface IAppGlobalSettingsRepositry
    {
        Task SaveAsync(IAppGlobalSettings appGlobalSettings);

        Task UpdateAsync(string depositUrl = null, bool? debugMode = null,
            string defaultIosAssetGroup = null, string defaultAssetGroupForOther = null,
            double? minVersionOnReview = null, string reviewIosGroup = null, bool? isOnReview = null,
            double? icoLkkSold = null, bool? isOnMaintenance = null, int? lowCashOutTimeout = null,
            int? lowCashOutLimit = null, bool? marginTradingEnabled = null);

        Task<IAppGlobalSettings> GetAsync();
    }


    public static class AppGlobalSettingsRepositry
    {
        public static async Task<IAppGlobalSettings> GetFromDbOrDefault(this IAppGlobalSettingsRepositry table)
        {
            return await table.GetAsync() ?? AppGlobalSettings.CreateDefault();
        }
    }

}
