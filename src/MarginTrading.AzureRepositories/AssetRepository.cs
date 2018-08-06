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
        private readonly INoSQLTableStorage<AssetRepository.AssetEntity> _tableStorage;
        private readonly IConvertService _convertService;

        public AssetRepository(INoSQLTableStorage<AssetRepository.AssetEntity> tableStorage,
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

        public async Task<IAsset> DeleteAsync(string assetId)
        {
            return Convert(await _tableStorage.DeleteAsync(AssetRepository.AssetEntity.GeneratePartitionKey(),
                AssetRepository.AssetEntity.GenerateRowKey(assetId)));
        }

        public async Task<IAsset> GetAsync(string assetId)
        {
            return Convert(await _tableStorage.GetDataAsync(AssetRepository.AssetEntity.GeneratePartitionKey(),
                AssetRepository.AssetEntity.GenerateRowKey(assetId)));
        }

        private static IReadOnlyList<IAsset> Convert(
            IEnumerable<AssetRepository.AssetEntity> assetEntities)
        {
            return assetEntities.ToList<IAsset>();
        }

        private AssetRepository.AssetEntity Convert(IAsset asset)
        {
            return _convertService.Convert<IAsset, AssetRepository.AssetEntity>(asset,
                o => o.ConfigureMap(MemberList.Source).ForMember(e => e.ETag, e => e.UseValue("*")));
        }

        public class AssetEntity : AzureTableEntity, IAsset
        {
            public AssetEntity()
            {
                PartitionKey = GeneratePartitionKey();
            }

            public string Id
            {
                get => RowKey;
                set => RowKey = value;
            }

            public string Name { get; set; }
            public int Accuracy { get; set; }


            public static string GeneratePartitionKey()
            {
                return "Asset";
            }

            public static string GenerateRowKey(string assetId)
            {
                return assetId;
            }
        }
    }
}
