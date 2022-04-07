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
        
        public async Task<(IOperationExecutionInfo<TData>, bool added)> GetOrAddAsync<TData>(
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
                
            // todo: Azure implementation is not used so far but it will be required to think on proper implementation if case Azure implementation is needed
            return (Convert<TData>(entity), false);
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

        public async Task<PaginatedResponse<OperationExecutionInfoWithPause<SpecialLiquidationOperationData>>> GetRfqAsync(int skip,
            int take,
            string id = null,
            string instrumentId = null,
            string accountId = null,
            List<SpecialLiquidationOperationState> states = null,
            DateTime? @from = null,
            DateTime? to = null)
        {
            throw new NotSupportedException("Azure is not supported");
        }

        public async Task Save<TData>(IOperationExecutionInfo<TData> executionInfo) where TData : class
        {
            var entity = Convert(executionInfo);
            entity.LastModified = _dateService.Now();
            await _tableStorage.ReplaceAsync(entity);
        }

        public async Task<IEnumerable<string>> FilterPositionsInSpecialLiquidationAsync(IEnumerable<string> positionIds)
        {
            throw new NotImplementedException();
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
    }
}