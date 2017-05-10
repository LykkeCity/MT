using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Core.Settings;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories.Settings
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

    public class AppGlobalSettingsRepository : IAppGlobalSettingsRepositry
    {

        private readonly INoSQLTableStorage<AppGlobalSettingsEntity> _tableStorage;

        public AppGlobalSettingsRepository(INoSQLTableStorage<AppGlobalSettingsEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task SaveAsync(IAppGlobalSettings appGlobalSettings)
        {
            var newEntity = AppGlobalSettingsEntity.Create(appGlobalSettings);
            return _tableStorage.InsertOrMergeAsync(newEntity);
        }

        public async Task UpdateAsync(string depositUrl = null, bool? debugMode = null,
            string defaultIosAssetGroup = null, string defaultAssetGroupForOther = null,
            double? minVersionOnReview = null, string reviewIosGroup = null, bool? isOnReview = null,
            double? icoLkkSold = null, bool? isOnMaintenance = null, int? lowCashOutTimeout = null,
            int? lowCashOutLimit = null, bool? marginTradingEnabled = null)
        {
            var entity =
                await
                    _tableStorage.GetDataAsync(AppGlobalSettingsEntity.GeneratePartitionKey(),
                        AppGlobalSettingsEntity.GenerateRowKey());

            entity.DebugMode = debugMode ?? entity.DebugMode;
            entity.DepositUrl = depositUrl;
            entity.DefaultIosAssetGroup = defaultIosAssetGroup;
            entity.DefaultAssetGroupForOther = defaultAssetGroupForOther;
            entity.MinVersionOnReview = minVersionOnReview ?? entity.MinVersionOnReview;
            entity.ReviewIosGroup = reviewIosGroup;
            entity.IsOnReview = isOnReview ?? entity.IsOnReview;
            entity.IcoLkkSold = icoLkkSold ?? entity.IcoLkkSold;
            entity.IsOnMaintenance = isOnMaintenance ?? entity.IsOnMaintenance;
            entity.LowCashOutLimit = lowCashOutLimit ?? entity.LowCashOutLimit;
            entity.LowCashOutTimeoutMins = lowCashOutTimeout ?? entity.LowCashOutTimeoutMins;
            entity.MarginTradingEnabled = marginTradingEnabled ?? entity.MarginTradingEnabled;

            await _tableStorage.InsertOrMergeAsync(entity);
        }

        public async Task<IAppGlobalSettings> GetAsync()
        {
            var partitionKey = AppGlobalSettingsEntity.GeneratePartitionKey();
            var rowKey = AppGlobalSettingsEntity.GenerateRowKey();
            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }

    }




}
