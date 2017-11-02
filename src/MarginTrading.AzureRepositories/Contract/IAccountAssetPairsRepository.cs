﻿using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.AzureRepositories.Contract
{
    public interface IAccountAssetPairsRepository
    {
        Task AddOrReplaceAsync(IAccountAssetPair accountAssetPair);
        Task<IAccountAssetPair> GetAsync(string tradingConditionId, string baseAssetId, string assetPairId);
        Task<IEnumerable<IAccountAssetPair>> GetAllAsync(string tradingConditionId, string baseAssetId);
        Task<IEnumerable<IAccountAssetPair>> GetAllAsync();
        Task AssignAssetPairs(string tradingConditionId, string baseAssetId, string[] assetPairsIds, AccountAssetsSettings defaults);
    }
}