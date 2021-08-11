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

        /// <summary>
        /// Checks the list of positions against database and returns the ones which are currently in
        /// Special Liquidation process or the process has been already successfully completed.
        /// Special Liquidation processes in all statuses except OnTheWayToFail and Failed are taken
        /// into consideration. 
        /// </summary>
        /// <param name="positions">The list of positions</param>
        /// <returns></returns>
        Task<IEnumerable<string>> FilterPositionsInSpecialLiquidationAsync(IEnumerable<string> positions);
    }
}