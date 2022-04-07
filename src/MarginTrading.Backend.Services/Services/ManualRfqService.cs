// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Rfq;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Extensions;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Services
{
    public class ManualRfqService : IRfqService
    {
        private readonly ICqrsSender _cqrsSender;
        private readonly IDateService _dateService;
        private readonly SpecialLiquidationSettings _specialLiquidationSettings;
        private readonly CqrsContextNamesSettings _cqrsContextNamesSettings;
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly ILog _log;

        private readonly ConcurrentDictionary<string, GetPriceForSpecialLiquidationCommand> _requests = new ConcurrentDictionary<string, GetPriceForSpecialLiquidationCommand>();

        public ManualRfqService(
            ICqrsSender cqrsSender,
            IDateService dateService,
            SpecialLiquidationSettings specialLiquidationSettings,
            CqrsContextNamesSettings cqrsContextNamesSettings,
            IQuoteCacheService quoteCacheService,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            ILog log)
        {
            _cqrsSender = cqrsSender;
            _dateService = dateService;
            _specialLiquidationSettings = specialLiquidationSettings;
            _cqrsContextNamesSettings = cqrsContextNamesSettings;
            _quoteCacheService = quoteCacheService;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _log = log;
        }
        
        public void SavePriceRequestForSpecialLiquidation(GetPriceForSpecialLiquidationCommand command)
        {
            _requests.TryAdd(command.OperationId, command);

            if (_specialLiquidationSettings.FakePriceRequestAutoApproval)
            {
                ApprovePriceRequest(command.OperationId, null);
                
                _log.WriteInfo(nameof(ManualRfqService), 
                    nameof(SavePriceRequestForSpecialLiquidation),
                    $"The price request for {command.OperationId} has been automatically approved according to configuration");
            }
        }

        public void RejectPriceRequest(string operationId, string reason)
        {
            _cqrsSender.PublishEvent(new PriceForSpecialLiquidationCalculationFailedEvent
            {
                OperationId = operationId,
                CreationTime = _dateService.Now(),
                Reason = reason
            }, _cqrsContextNamesSettings.Gavel);
            
            _requests.TryRemove(operationId, out _);
        }

        public void ApprovePriceRequest(string operationId, decimal? price)
        {
            if (_specialLiquidationSettings.FakePriceRequestAutoApproval)
            {
                _log.WriteWarning(nameof(ManualRfqService), 
                    nameof(ApprovePriceRequest),
                    $"Most probably, the price request for {operationId} has already been automatically approved according to configuration");
            }
            
            if (!_requests.TryGetValue(operationId, out var command))
            {
                throw new InvalidOperationException($"Command with operation ID {operationId} does not exist");
            }
            
            if (price == null)
            {
                var quote = _quoteCacheService.GetQuote(command.Instrument);

                price = (command.Volume > 0 ? quote.Ask : quote.Bid) * _specialLiquidationSettings.FakePriceMultiplier;
            }

            _cqrsSender.PublishEvent(new PriceForSpecialLiquidationCalculatedEvent
            {
                OperationId = operationId,
                CreationTime = _dateService.Now(),
                Price = price.Value,
            }, _cqrsContextNamesSettings.Gavel);
            
            _requests.TryRemove(operationId, out _);
        }

        public List<GetPriceForSpecialLiquidationCommand> GetAllRequest()
        {
            return _requests.Values.ToList();
        }

        public async Task<PaginatedResponse<Rfq>> GetAsync(RfqFilter filter, int skip, int take)
        {
            var specialLiquidationStates = filter?.States?
                .Select(x => (SpecialLiquidationOperationState)x)
                .ToList();

            var filteredRfq = await _operationExecutionInfoRepository
                .GetRfqAsync(skip,
                    take,
                    filter?.OperationId,
                    filter?.InstrumentId,
                    filter?.AccountId,
                    specialLiquidationStates,
                    filter?.DateFrom,
                    filter?.DateTo);

            var pauseFilterAppliedRfq = filteredRfq.Contents
                .Select(o => o.ToRfq())
                .Where(GetApplyPauseFilterFunc(filter));

            return new PaginatedResponse<Rfq>(
                pauseFilterAppliedRfq.ToList(),
                filteredRfq.Start,
                filteredRfq.Size,
                filteredRfq.TotalSize);
        }

        private static Func<Rfq, bool> GetApplyPauseFilterFunc(RfqFilter filter)
        {
            return o =>
            {
                if (filter == null)
                    return true;

                return (filter.CanBePaused.HasValue && o.PauseSummary.CanBePaused == filter.CanBePaused) ||
                       (filter.CanBeResumed.HasValue && o.PauseSummary.CanBeResumed == filter.CanBeResumed) ||
                       (filter.CanBeStopped.HasValue && o.PauseSummary.CanBeStopped == filter.CanBeStopped);
            };
        }
    }
}