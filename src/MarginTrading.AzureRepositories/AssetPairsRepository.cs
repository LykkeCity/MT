using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AzureStorage;
using Lykke.AzureStorage.Tables;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Common.Services;

namespace MarginTrading.AzureRepositories
{
    internal class AssetPairsRepository : IAssetPairsRepository
    {
        private readonly INoSQLTableStorage<AssetPairEntity> _tableStorage;
        private readonly IConvertService _convertService;

        public AssetPairsRepository(INoSQLTableStorage<AssetPairEntity> tableStorage,
            IConvertService convertService)
        {
            _tableStorage = tableStorage;
            _convertService = convertService;
        }

        public async Task<IReadOnlyList<IAssetPair>> GetAsync()
        {
            return Convert(await _tableStorage.GetDataAsync());
        }

        public Task InsertAsync(IAssetPair settings)
        {
            return _tableStorage.InsertAsync(Convert(settings));
        }

        public Task ReplaceAsync(IAssetPair settings)
        {
            return _tableStorage.ReplaceAsync(Convert(settings));
        }

        public async Task<IAssetPair> DeleteAsync(string assetPairId)
        {
            return Convert(await _tableStorage.DeleteAsync(AssetPairEntity.GeneratePartitionKey(),
                AssetPairEntity.GenerateRowKey(assetPairId)));
        }

        public async Task<IAssetPair> GetAsync(string assetPairId)
        {
            return Convert(await _tableStorage.GetDataAsync(AssetPairEntity.GeneratePartitionKey(),
                AssetPairEntity.GenerateRowKey(assetPairId)));
        }

        private static IReadOnlyList<IAssetPair> Convert(
            IEnumerable<AssetPairEntity> accountAssetPairEntities)
        {
            return accountAssetPairEntities.ToList<IAssetPair>();
        }

        private AssetPairEntity Convert(IAssetPair accountAssetPair)
        {
            return _convertService.Convert<IAssetPair, AssetPairEntity>(accountAssetPair,
                o => o.ConfigureMap(MemberList.Source).ForMember(e => e.ETag, e => e.UseValue("*")));
        }
        
        internal class AssetPairEntity : AzureTableEntity, IAssetPair
        {
            public AssetPairEntity()
            {
                PartitionKey = GeneratePartitionKey();
            }

            public string Id
            {
                get => RowKey;
                set => RowKey = value;
            }

            public string Name { get; set; }
            public string BaseAssetId { get; set; }
            public string QuoteAssetId { get; set; }
            public int Accuracy { get; set; }

            public string LegalEntity { get; set; }
            public string BasePairId { get; set; }
            public MatchingEngineMode MatchingEngineMode { get; set; }
            public decimal StpMultiplierMarkupBid { get; set; }
            public decimal StpMultiplierMarkupAsk { get; set; }

            public static string GeneratePartitionKey()
            {
                return "AssetPairs";
            }

            public static string GenerateRowKey(string assetPairId)
            {
                return assetPairId;
            }
        }
    }
}