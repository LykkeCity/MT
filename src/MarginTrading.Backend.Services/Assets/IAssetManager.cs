using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Assets
{
    public interface IAssetManager
    {
        Task<IAsset> UpdateAsset(IAsset asset);
        Task<IAsset> InsertAsset(IAsset asset);
        Task<IAsset> DeleteAsset(string assetId);
    }
}
