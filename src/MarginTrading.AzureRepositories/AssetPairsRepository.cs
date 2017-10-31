using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Backend.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class AssetPairEntity : TableEntity, IAssetPair
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

        public static AssetPairEntity Create(IAssetPair src)
        {
            return new AssetPairEntity
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

    public class AssetPairsRepository : IAssetPairsRepository
    {
        private readonly INoSQLTableStorage<AssetPairEntity> _tableStorage;

        public AssetPairsRepository(INoSQLTableStorage<AssetPairEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IEnumerable<IAssetPair>> GetAllAsync()
        {
            var partitionKey = AssetPairEntity.GeneratePartitionKey();
            return await _tableStorage.GetDataAsync(partitionKey);
        }

        public async Task<IEnumerable<IAssetPair>> GetAllAsync(List<string> instruments)
        {
            return await _tableStorage.GetDataAsync(item => instruments.Contains(item.Id));
        }

        public async Task AddAsync(IAssetPair assetPair)
        {
            await _tableStorage.InsertOrReplaceAsync(AssetPairEntity.Create(assetPair));
        }

        public async Task<IAssetPair> GetAssetAsync(string assetId)
        {
            return await _tableStorage.GetDataAsync(AssetPairEntity.GeneratePartitionKey(), assetId);
        }
    }
}
