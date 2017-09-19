using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.MarketMaker.AzureRepositories;
using MarginTrading.MarketMaker.AzureRepositories.Entities;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using Rocks.Caching;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    internal class AssetPairsSettingsService : CachedEntityAccessorService<AssetPairSettingsEntity>,
        IAssetPairsSettingsService
    {
        private readonly IAssetsPairsSettingsRepository _assetsPairsSettingsRepository;

        public AssetPairsSettingsService(ICacheProvider cacheProvider,
            IAssetsPairsSettingsRepository assetsPairsSettingsRepository)
            : base(cacheProvider, assetsPairsSettingsRepository)
        {
            _assetsPairsSettingsRepository = assetsPairsSettingsRepository;
        }

        public Task SetAssetPairQuotesSource(string assetPairId, AssetPairQuotesSourceTypeEnum assetPairQuotesSourceType, string externalExchange)
        {
            return UpdateByKey(GetKeys(assetPairId), e =>
            {
                e.QuotesSourceType = assetPairQuotesSourceType;
                if (externalExchange != null)
                {
                    e.ExternalExchange = externalExchange;
                }
            });
        }

        public async Task<IReadOnlyDictionary<string, string>> GetAllPairsSources()
        {
            return (await _assetsPairsSettingsRepository.GetAll())
                .ToDictionary(s => s.AssetName, c => c.QuotesSourceType.ToString());
        }

        public (AssetPairQuotesSourceTypeEnum? SourceType, string ExternalExchange) GetAssetPairQuotesSource(string assetPairId)
        {
            var entity = GetByKey(GetKeys(assetPairId));
            return (entity?.QuotesSourceType, entity?.ExternalExchange);
        }

        private static EntityKeys GetKeys(string assetPairId)
        {
            return new EntityKeys(AssetPairSettingsEntity.GeneratePartitionKey(), AssetPairSettingsEntity
                .GenerateRowKey(assetPairId));
        }
    }
}