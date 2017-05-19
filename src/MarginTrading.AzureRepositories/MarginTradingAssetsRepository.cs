using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingAssetEntity : TableEntity, IMarginTradingAsset
    {
        public string Id => RowKey;
        public string Name { get; set; }
        public string BaseAssetId { get; set; }
        public string QuoteAssetId { get; set; }
        public int Accuracy { get; set; }

        public static string GeneratePartitionKey()
        {
            return "MarginTradingAsset";
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }

        public static MarginTradingAssetEntity Create(IMarginTradingAsset src)
        {
            return new MarginTradingAssetEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(src.Id),
                Name = src.Name,
                BaseAssetId = src.BaseAssetId,
                QuoteAssetId = src.QuoteAssetId,
                Accuracy = src.Accuracy
            };
        }
    }

    public class MarginTradingAssetsRepository : IMarginTradingAssetsRepository
    {
        private readonly INoSQLTableStorage<MarginTradingAssetEntity> _tableStorage;

        public MarginTradingAssetsRepository(INoSQLTableStorage<MarginTradingAssetEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IEnumerable<IMarginTradingAsset>> GetAllAsync()
        {
            var partitionKey = MarginTradingAssetEntity.GeneratePartitionKey();
            var assets = await _tableStorage.GetDataAsync(partitionKey);

            return assets.Select(MarginTradingAsset.Create);
        }

        public async Task<IEnumerable<MarginTradingAsset>> GetAllAsync(List<string> instruments)
        {
            var assets = await _tableStorage.GetDataAsync(item => instruments.Contains(item.Id));

            return assets.Select(MarginTradingAsset.Create);
        }

        public async Task AddAsync(IMarginTradingAsset asset)
        {
            await _tableStorage.InsertOrReplaceAsync(MarginTradingAssetEntity.Create(asset));
        }

        public async Task<IMarginTradingAsset> GetAssetAsync(string assetId)
        {
            return await _tableStorage.GetDataAsync(MarginTradingAssetEntity.GeneratePartitionKey(), assetId);
        }
    }
}
