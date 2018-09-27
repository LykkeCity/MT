using System;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Lykke.Common;
using Lykke.Service.ExchangeConnector.Client;
using Lykke.Service.ExchangeConnector.Client.Models;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Services
{
    public class SpecialLiquidationService : ISpecialLiquidationService
    {
        private readonly ICqrsSender _cqrsSender;
        private readonly IDateService _dateService;
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly SpecialLiquidationSettings _specialLiquidationSettings;
        private readonly CqrsContextNamesSettings _cqrsContextNamesSettings;

        public SpecialLiquidationService(
            ICqrsSender cqrsSender,
            IDateService dateService,
            IThreadSwitcher threadSwitcher,
            SpecialLiquidationSettings specialLiquidationSettings,
            CqrsContextNamesSettings cqrsContextNamesSettings)
        {
            _cqrsSender = cqrsSender;
            _dateService = dateService;
            _threadSwitcher = threadSwitcher;
            _specialLiquidationSettings = specialLiquidationSettings;
            _cqrsContextNamesSettings = cqrsContextNamesSettings;
        }
        
        public void FakeGetPriceForSpecialLiquidation(string operationId, string instrument, decimal volume)
        {
            _threadSwitcher.SwitchThread(async () =>
            {
                await Task.Delay(1000);//waiting for the state to be saved into the repo
                
                _cqrsSender.PublishEvent(new PriceForSpecialLiquidationCalculatedEvent
                {
                    OperationId = operationId,
                    CreationTime = _dateService.Now(),
                    Instrument = instrument,
                    Volume = volume,
                    Price = _specialLiquidationSettings.FakePrice,
                }, _cqrsContextNamesSettings.Gavel);
            });
        }

        public void FakeExecuteSpecialLiquidationOrder(string operationId, string instrument, decimal volume, decimal price)
        {
            _threadSwitcher.SwitchThread(async () =>
            {
                await Task.Delay(1000);//waiting for the state to be saved into the repo

                _cqrsSender.PublishEvent(new SpecialLiquidationOrderExecutedEvent
                {
                    OperationId = operationId,
                    CreationTime = _dateService.Now(),
                }, _cqrsContextNamesSettings.Gavel);
            });
        }
    }
}