using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class AssetRepository : IAssetRepository
    {
        private readonly INoSQLTableStorage<AssetPairsRepository.AssetPairEntity> _tableStorage;
        private readonly IConvertService _convertService;

        public AssetRepository(INoSQLTableStorage<AssetPairsRepository.AssetPairEntity> tableStorage,
            IConvertService convertService)
        {
            _tableStorage = tableStorage;
            _convertService = convertService;
        }

        public async Task<IReadOnlyList<IAsset>> GetAsync()
        {
            return Convert(await _tableStorage.GetDataAsync());
        }

        public Task InsertAsync(IAsset settings)
        {
            return _tableStorage.InsertAsync(Convert(settings));
        }

        public Task ReplaceAsync(IAsset settings)
        {
            return _tableStorage.ReplaceAsync(Convert(settings));
        }

        public async Task<IAsset> DeleteAsync(string assetPairId)
        {
            return Convert(await _tableStorage.DeleteAsync(AssetPairsRepository.AssetPairEntity.GeneratePartitionKey(),
                AssetPairsRepository.AssetPairEntity.GenerateRowKey(assetPairId)));
        }

        public async Task<IAsset> GetAsync(string assetPairId)
        {
            return Convert(await _tableStorage.GetDataAsync(AssetPairsRepository.AssetPairEntity.GeneratePartitionKey(),
                AssetPairsRepository.AssetPairEntity.GenerateRowKey(assetPairId)));
        }

        private static IReadOnlyList<IAssetPair> Convert(
            IEnumerable<AssetPairsRepository.AssetPairEntity> accountAssetPairEntities)
        {
            return accountAssetPairEntities.ToList<IAssetPair>();
        }

        private AssetPairsRepository.AssetPairEntity Convert(IAssetPair accountAssetPair)
        {
            return _convertService.Convert<IAssetPair, AssetPairsRepository.AssetPairEntity>(accountAssetPair,
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
