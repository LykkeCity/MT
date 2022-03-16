// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services.Extensions;
using MarginTrading.Backend.Services.Notifications;

namespace MarginTrading.Backend.Services.Services
{
    [UsedImplicitly]
    public class RfqExecutionInfoRepositoryDecorator : IOperationExecutionInfoRepository
    {
        private readonly IOperationExecutionInfoRepository _decoratee;
        private readonly IRabbitMqNotifyService _notifyService;
        private readonly ILog _log;

        public RfqExecutionInfoRepositoryDecorator(IOperationExecutionInfoRepository decoratee, ILog log, IRabbitMqNotifyService notifyService)
        {
            _decoratee = decoratee;
            _log = log;
            _notifyService = notifyService;
        }

        public async Task<(IOperationExecutionInfo<TData>, bool added)> GetOrAddAsync<TData>(string operationName, string operationId, Func<IOperationExecutionInfo<TData>> factory) where TData : class
        {
            var (executionInfo, added) = await _decoratee.GetOrAddAsync(operationName, operationId, factory);

            if (added)
            {
                await _log.WriteInfoAsync(nameof(RfqExecutionInfoRepositoryDecorator),
                    nameof(GetOrAddAsync),
                    new { Id = operationId, Name = operationName }.ToJson(),
                    $"New RFQ has been added therefore {nameof(RfqChangedEvent)} is about to be published");

                var rfq = await GetRfqByIdAsync(operationId);

                await _notifyService.RfqChanged(rfq.ToEventContract());
            }
            
            return (executionInfo, added);
        }

        public Task<IOperationExecutionInfo<TData>> GetAsync<TData>(string operationName, string id) where TData : class
        {
            return _decoratee.GetAsync<TData>(operationName, id);
        }

        public Task<PaginatedResponse<OperationExecutionInfoWithPause<SpecialLiquidationOperationData>>> GetRfqAsync(string rfqId,
            string instrumentId,
            string accountId,
            List<SpecialLiquidationOperationState> states,
            DateTime? @from,
            DateTime? to,
            int skip,
            int take,
            bool isAscendingOrder = false)
        {
            return _decoratee.GetRfqAsync(rfqId, instrumentId, accountId, states, @from, to, skip, take, isAscendingOrder);
        }

        public async Task Save<TData>(IOperationExecutionInfo<TData> executionInfo) where TData : class
        {
            await _decoratee.Save(executionInfo);
            
            await _log.WriteInfoAsync(nameof(RfqExecutionInfoRepositoryDecorator),
                nameof(GetOrAddAsync),
                new { Id = executionInfo.Id, Name = executionInfo.OperationName }.ToJson(),
                $"RFQ has been updated therefore {nameof(RfqChangedEvent)} is about to be published");

            var rfq = await GetRfqByIdAsync(executionInfo.Id);

            await _notifyService.RfqChanged(rfq.ToEventContract());
        }

        public Task<IEnumerable<string>> FilterPositionsInSpecialLiquidationAsync(IEnumerable<string> positionIds)
        {
            return _decoratee.FilterPositionsInSpecialLiquidationAsync(positionIds);
        }

        private async Task<OperationExecutionInfoWithPause<SpecialLiquidationOperationData>> GetRfqByIdAsync(string id)
        {
            return (await _decoratee
                    .GetRfqAsync(id, null, null, null, null, null, 0, 1))
                .Contents
                .Single();
        }
    }
}