using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.MarketMaker.AzureRepositories.Entities;
using MarginTrading.MarketMaker.Enums;

namespace MarginTrading.MarketMaker.AzureRepositories
{
    internal interface IAssetsPairsSettingsRepository: IEntityRepository<AssetPairSettingsEntity>
    {
        Task<IList<AssetPairSettingsEntity>> GetAll();
    }
}