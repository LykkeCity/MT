// Copyright (c) 2021 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core.Repositories
{
    public interface IOperationExecutionInfoRepository
    {
        Task<IOperationExecutionInfo<TData>> GetOrAddAsync<TData>(string operationName,
            string operationId, Func<IOperationExecutionInfo<TData>> factory) where TData : class;

        [ItemCanBeNull]
        Task<IOperationExecutionInfo<TData>> GetAsync<TData>(string operationName, string id) where TData : class;

        Task<PaginatedResponse<OperationExecutionInfo<SpecialLiquidationOperationData>>> GetRfqAsync(string rfqId, 
            string instrumentId, string accountId, List<SpecialLiquidationOperationState> states, DateTime? from, DateTime? to, 
            int skip, int take, bool isAscendingOrder = true);

        Task Save<TData>(IOperationExecutionInfo<TData> executionInfo) where TData : class;
    }
}