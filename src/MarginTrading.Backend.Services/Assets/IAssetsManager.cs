// Copyright (c) 2019 Lykke Corp.

using System.Threading.Tasks;

namespace MarginTrading.Backend.Services.Assets
{
    public interface IAssetsManager
    {
        Task UpdateCacheAsync();
    }
}