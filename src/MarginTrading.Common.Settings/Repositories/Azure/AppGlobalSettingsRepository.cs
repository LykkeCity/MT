using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Common.Settings.Models;
using MarginTrading.Common.Settings.Repositories.Azure.Entities;

namespace MarginTrading.Common.Settings.Repositories.Azure
{
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
