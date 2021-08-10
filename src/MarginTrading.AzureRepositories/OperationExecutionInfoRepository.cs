// Copyright (c) 2021 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AzureRepositories.Entities;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Common.Services;
using Microsoft.Extensions.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MarginTrading.AzureRepositories
{
    public class OperationExecutionInfoRepository : IOperationExecutionInfoRepository
    {
        private readonly INoSQLTableStorage<OperationExecutionInfoEntity> _tableStorage;
        private readonly IDateService _dateService;

        public OperationExecutionInfoRepository(IReloadingManager<string> connectionStringManager, 
            ILog log, IDateService dateService)
        {
            _tableStorage = AzureTableStorage<OperationExecutionInfoEntity>.Create(
                connectionStringManager,
                "MarginTradingExecutionInfo",
                log);
            _dateService = dateService;
        }
        
        public async Task<IOperationExecutionInfo<TData>> GetOrAddAsync<TData>(
            string operationName, string operationId, Func<IOperationExecutionInfo<TData>> factory) where TData : class
        {
            var entity = await _tableStorage.GetOrInsertAsync(
                partitionKey: OperationExecutionInfoEntity.GeneratePartitionKey(operationName),
                rowKey: OperationExecutionInfoEntity.GeneratePartitionKey(operationId),
                createNew: () =>
                {
                    var result = Convert(factory());
                    result.LastModified = _dateService.Now();
                    return result;
                });
                
            return Convert<TData>(entity);
        }

        public async Task<IOperationExecutionInfo<TData>> GetAsync<TData>(string operationName, string id)
            where TData : class
        {
            var obj = await _tableStorage.GetDataAsync(
                          OperationExecutionInfoEntity.GeneratePartitionKey(operationName),
                          OperationExecutionInfoEntity.GenerateRowKey(id)) ?? throw new InvalidOperationException(
                          $"Operation execution info for {operationName} #{id} not yet exists");
            
            return Convert<TData>(obj);
        }

        public async Task Save<TData>(IOperationExecutionInfo<TData> executionInfo) where TData : class
        {
            var entity = Convert(executionInfo);
            entity.LastModified = _dateService.Now();
            await _tableStorage.ReplaceAsync(entity);
        }

        private static IOperationExecutionInfo<TData> Convert<TData>(OperationExecutionInfoEntity entity)
            where TData : class
        {
            return new OperationExecutionInfo<TData>(
                operationName: entity.OperationName,
                id: entity.Id,
                lastModified: entity.LastModified,
                data: entity.Data is string dataStr
                    ? JsonConvert.DeserializeObject<TData>(dataStr)
                    : ((JToken) entity.Data).ToObject<TData>());
        }

        private static OperationExecutionInfoEntity Convert<TData>(IOperationExecutionInfo<TData> model)
            where TData : class
        {
            return new OperationExecutionInfoEntity
            {
                Id = model.Id,
                OperationName = model.OperationName,
                Data = model.Data.ToJson(),
            };
        }

        public Task<PaginatedResponse<OperationExecutionInfo<SpecialLiquidationOperationData>>> GetRfqAsync(string rfqId, string instrumetnId, string accountId, List<SpecialLiquidationOperationState> states, DateTime? from, DateTime? to, int skip, int take, bool isAscendingOrder = true)
        {
            throw new NotSupportedException("Azure is not supported");
        }
    }
}