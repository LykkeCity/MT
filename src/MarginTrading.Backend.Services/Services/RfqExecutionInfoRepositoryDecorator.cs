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
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Extensions;
using MarginTrading.Backend.Services.Notifications;

namespace MarginTrading.Backend.Services.Services
{
    [UsedImplicitly]
    public class RfqExecutionInfoRepositoryDecorator : IOperationExecutionInfoRepository
    {
        private readonly IOperationExecutionInfoRepository _decoratee;
        private readonly IRabbitMqNotifyService _notifyService;
        private readonly string _brokerId;
        private readonly ILog _log;

        private static readonly Type SpecialLiquidationDataType = typeof(SpecialLiquidationOperationData);

        public RfqExecutionInfoRepositoryDecorator(IOperationExecutionInfoRepository decoratee,
            ILog log,
            IRabbitMqNotifyService notifyService,
            MarginTradingSettings settings)
        {
            _decoratee = decoratee;
            _log = log;
            _notifyService = notifyService;
            _brokerId = settings.BrokerId;
        }

        public async Task<(IOperationExecutionInfo<TData>, bool added)> GetOrAddAsync<TData>(string operationName, string operationId, Func<IOperationExecutionInfo<TData>> factory) where TData : class
        {
            var (executionInfo, added) = await _decoratee.GetOrAddAsync(operationName, operationId, factory);

            if (added && typeof(TData) == SpecialLiquidationDataType)
            {
                await _log.WriteInfoAsync(nameof(RfqExecutionInfoRepositoryDecorator),
                    nameof(GetOrAddAsync),
                    new { Id = operationId, Name = operationName }.ToJson(),
                    $"New RFQ has been added therefore {nameof(RfqEvent)} is about to be published");

                var rfq = await GetRfqByIdAsync(operationId);

                await _notifyService.Rfq(rfq.ToEventContract(RfqEventTypeContract.New, _brokerId));
            }
            
            return (executionInfo, added);
        }

        public Task<IOperationExecutionInfo<TData>> GetAsync<TData>(string operationName, string id) where TData : class
        {
            return _decoratee.GetAsync<TData>(operationName, id);
        }

        public Task<PaginatedResponse<OperationExecutionInfoWithPause<SpecialLiquidationOperationData>>> GetRfqAsync(int skip,
            int take,
            string id = null,
            string instrumentId = null,
            string accountId = null,
            List<SpecialLiquidationOperationState> states = null,
            DateTime? @from = null,
            DateTime? to = null)
        {
            return _decoratee.GetRfqAsync(skip, take, id, instrumentId, accountId, states, @from, to);
        }

        public async Task Save<TData>(IOperationExecutionInfo<TData> executionInfo) where TData : class
        {
            await _decoratee.Save(executionInfo);

            if (typeof(TData) == SpecialLiquidationDataType)
            {
                await _log.WriteInfoAsync(nameof(RfqExecutionInfoRepositoryDecorator),
                    nameof(GetOrAddAsync),
                    new { executionInfo.Id, Name = executionInfo.OperationName }.ToJson(),
                    $"RFQ has been updated therefore {nameof(RfqEvent)} is about to be published");

                var rfq = await GetRfqByIdAsync(executionInfo.Id);

                await _notifyService.Rfq(rfq.ToEventContract(RfqEventTypeContract.Update, _brokerId));
            }
        }

        public Task<IEnumerable<string>> FilterPositionsInSpecialLiquidationAsync(IEnumerable<string> positionIds)
        {
            return _decoratee.FilterPositionsInSpecialLiquidationAsync(positionIds);
        }

        private async Task<OperationExecutionInfoWithPause<SpecialLiquidationOperationData>> GetRfqByIdAsync(string id)
        {
            return (await _decoratee
                    .GetRfqAsync(0, 1, id))
                .Contents
                .Single();
        }
    }
}