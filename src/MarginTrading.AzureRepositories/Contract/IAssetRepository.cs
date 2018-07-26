using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;

namespace MarginTrading.AzureRepositories.Contract
{
    public interface IAssetRepository
    {
        Task<IReadOnlyList<IAsset>> GetAsync();
        Task InsertAsync(IAsset settings);
        Task ReplaceAsync(IAsset settings);
        Task<IAsset> DeleteAsync(string assetId);
        Task<IAsset> GetAsync(string assetId);
    }
}
