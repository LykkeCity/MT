using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingAssetPairEntity : TableEntity, IMarginTradingAssetPair
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

        public static MarginTradingAssetPairEntity Create(IMarginTradingAssetPair src)
        {
            return new MarginTradingAssetPairEntity
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

    public class MarginTradingAssetPairsRepository : IMarginTradingAssetPairsRepository
    {
        private readonly INoSQLTableStorage<MarginTradingAssetPairEntity> _tableStorage;

        public MarginTradingAssetPairsRepository(INoSQLTableStorage<MarginTradingAssetPairEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IEnumerable<IMarginTradingAssetPair>> GetAllAsync()
        {
            var partitionKey = MarginTradingAssetPairEntity.GeneratePartitionKey();
            var assets = await _tableStorage.GetDataAsync(partitionKey);

            return assets.Select(MarginTradingAssetPair.Create);
        }

        public async Task<IEnumerable<MarginTradingAssetPair>> GetAllAsync(List<string> instruments)
        {
            var assets = await _tableStorage.GetDataAsync(item => instruments.Contains(item.Id));

            return assets.Select(MarginTradingAssetPair.Create);
        }

        public async Task AddAsync(IMarginTradingAssetPair assetPair)
        {
            await _tableStorage.InsertOrReplaceAsync(MarginTradingAssetPairEntity.Create(assetPair));
        }

        public async Task<IMarginTradingAssetPair> GetAssetAsync(string assetId)
        {
            return await _tableStorage.GetDataAsync(MarginTradingAssetPairEntity.GeneratePartitionKey(), assetId);
        }
    }
}
