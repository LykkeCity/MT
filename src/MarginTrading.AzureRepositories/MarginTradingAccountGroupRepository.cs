﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingAccountGroupEntity : TableEntity, IMarginTradingAccountGroup
    {
        public string TradingConditionId => PartitionKey;
        public string BaseAssetId => RowKey;

        public double MarginCall { get; set; }
        public double StopOut { get; set; }
        public double DepositTransferLimit { get; set; }

        public static string GeneratePartitionKey(string tradingConditionId)
        {
            return tradingConditionId;
        }

        public static string GenerateRowKey(string baseAssetid)
        {
            return baseAssetid;
        }

        public static MarginTradingAccountGroupEntity Create(IMarginTradingAccountGroup src)
        {
            return new MarginTradingAccountGroupEntity
            {
                PartitionKey = GeneratePartitionKey(src.TradingConditionId),
                RowKey = GenerateRowKey(src.BaseAssetId),
                MarginCall = src.MarginCall,
                StopOut = src.StopOut,
                DepositTransferLimit = src.DepositTransferLimit
            };
        }
    }

    public class MarginTradingAccountGroupRepository : IMarginTradingAccountGroupRepository
    {
        private readonly INoSQLTableStorage<MarginTradingAccountGroupEntity> _tableStorage;

        public MarginTradingAccountGroupRepository(INoSQLTableStorage<MarginTradingAccountGroupEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddOrReplaceAsync(IMarginTradingAccountGroup group)
        {
            await _tableStorage.InsertOrReplaceAsync(MarginTradingAccountGroupEntity.Create(group));
        }

        public async Task<IMarginTradingAccountGroup> GetAsync(string tradingConditionId, string baseAssetId)
        {
            var entity = await _tableStorage.GetDataAsync(MarginTradingAccountGroupEntity.GeneratePartitionKey(tradingConditionId),
                MarginTradingAccountGroupEntity.GenerateRowKey(baseAssetId));

            return entity != null
                ? MarginTradingAccountGroupEntity.Create(entity)
                : null;
        }

        public async Task<IEnumerable<IMarginTradingAccountGroup>> GetAllAsync()
        {
            var entity = await _tableStorage.GetDataAsync();

            return entity.Any()
                ? entity.Select(MarginTradingAccountGroup.Create)
                : new List<IMarginTradingAccountGroup>();
        }
    }
}
