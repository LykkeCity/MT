using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.MarketMaker.AzureRepositories.Entities;

namespace MarginTrading.MarketMaker.AzureRepositories
{
    internal interface IAssetsPairsSettingsRepository: IEntityRepository<AssetPairSettingsEntity>
    {
        Task<IList<AssetPairSettingsEntity>> GetAll();
        Task DeleteAsync(string partitionKey, string rowKey);
    }
}