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
    internal class AssetPairSettingsEntity : AzureTableEntity, IAssetPairSettings
    {
        public AssetPairSettingsEntity()
        {
            PartitionKey = GeneratePartitionKey();
        }

        public string AssetPairId
        {
            get => RowKey;
            set => RowKey = value;
        }
        
        public string LegalEntity { get; set; }
        public string BasePairId { get; set; }
        public MatchingEngineMode MatchingEngineMode { get; set; }
        public decimal MultiplierMarkupBid { get; set; }
        public decimal MultiplierMarkupAsk { get; set; }

        public static string GeneratePartitionKey()
        {
            return "AssetPairSettings";
        }
            
        public static string GenerateRowKey(string assetPairId)
        {
            return assetPairId;
        }
    }

    internal class AssetPairSettingsRepository : IAssetPairSettingsRepository
    {
        private readonly INoSQLTableStorage<AssetPairSettingsEntity> _tableStorage;
        private readonly IConvertService _convertService;

        public AssetPairSettingsRepository(INoSQLTableStorage<AssetPairSettingsEntity> tableStorage,
            IConvertService convertService)
        {
            _tableStorage = tableStorage;
            _convertService = convertService;
        }

        public async Task<IReadOnlyList<IAssetPairSettings>> Get()
        {
            return Convert(await _tableStorage.GetDataAsync());
        }

        public Task Insert(IAssetPairSettings settings)
        {
            return _tableStorage.InsertAsync(Convert(settings));
        }

        public Task Update(IAssetPairSettings settings)
        {
            return _tableStorage.ReplaceAsync(Convert(settings));
        }

        public async Task<IAssetPairSettings> Delete(string assetPairId)
        {
            return Convert(await _tableStorage.DeleteAsync(AssetPairSettingsEntity.GeneratePartitionKey(),
                AssetPairSettingsEntity.GenerateRowKey(assetPairId)));
        }

        public async Task<IAssetPairSettings> Get(string assetPairId)
        {
            return Convert(await _tableStorage.GetDataAsync(AssetPairSettingsEntity.GeneratePartitionKey(),
                AssetPairSettingsEntity.GenerateRowKey(assetPairId)));
        }

        private static IReadOnlyList<IAssetPairSettings> Convert(IEnumerable<AssetPairSettingsEntity> accountAssetPairEntities)
        {
            return accountAssetPairEntities.ToList<IAssetPairSettings>();
        }

        private AssetPairSettingsEntity Convert(IAssetPairSettings accountAssetPair)
        {
            return _convertService.Convert<IAssetPairSettings, AssetPairSettingsEntity>(accountAssetPair,
                o => o.ConfigureMap(MemberList.Source).ForMember(e => e.ETag, e => e.UseValue("*")));
        }
    }
}