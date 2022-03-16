// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTrading.SqlRepositories.Repositories
{
    [UsedImplicitly]
    public class RfqExecutionInfoRepositoryDecorator : IOperationExecutionInfoRepository
    {
        private readonly IOperationExecutionInfoRepository _decoratee;
        private readonly ILog _log;

        public RfqExecutionInfoRepositoryDecorator(IOperationExecutionInfoRepository decoratee, ILog log)
        {
            _decoratee = decoratee;
            _log = log;
        }

        public Task<IOperationExecutionInfo<TData>> GetOrAddAsync<TData>(string operationName, string operationId, Func<IOperationExecutionInfo<TData>> factory) where TData : class
        {
            return _decoratee.GetOrAddAsync(operationName, operationId, factory);
        }

        public Task<IOperationExecutionInfo<TData>> GetAsync<TData>(string operationName, string id) where TData : class
        {
            return _decoratee.GetAsync<TData>(operationName, id);
        }

        public async Task<PaginatedResponse<OperationExecutionInfoWithPause<SpecialLiquidationOperationData>>> GetRfqAsync(string rfqId,
            string instrumentId,
            string accountId,
            List<SpecialLiquidationOperationState> states,
            DateTime? @from,
            DateTime? to,
            int skip,
            int take,
            bool isAscendingOrder = false)
        {
            await _log.WriteInfoAsync(nameof(RfqExecutionInfoRepositoryDecorator), nameof(GetRfqAsync),
                new { operationId = rfqId }.ToJson(), "GetRfq request handled in decorator");
            
            return await _decoratee.GetRfqAsync(rfqId, instrumentId, accountId, states, @from, to, skip, take, isAscendingOrder);
        }

        public Task Save<TData>(IOperationExecutionInfo<TData> executionInfo) where TData : class
        {
            return _decoratee.Save(executionInfo);
        }

        public Task<IEnumerable<string>> FilterPositionsInSpecialLiquidationAsync(IEnumerable<string> positionIds)
        {
            return _decoratee.FilterPositionsInSpecialLiquidationAsync(positionIds);
        }
    }
}