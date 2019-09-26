// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lykke.Common;
using Lykke.Common.Chaos;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core;
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
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly IChaosKitty _chaosKitty;

        public SpecialLiquidationService(
            ICqrsSender cqrsSender,
            IDateService dateService,
            IThreadSwitcher threadSwitcher,
            SpecialLiquidationSettings specialLiquidationSettings,
            CqrsContextNamesSettings cqrsContextNamesSettings,
            IQuoteCacheService quoteCacheService,
            IChaosKitty chaosKitty)
        {
            _cqrsSender = cqrsSender;
            _dateService = dateService;
            _threadSwitcher = threadSwitcher;
            _specialLiquidationSettings = specialLiquidationSettings;
            _cqrsContextNamesSettings = cqrsContextNamesSettings;
            _quoteCacheService = quoteCacheService;
            _chaosKitty = chaosKitty;
        }
        
        public void FakeGetPriceForSpecialLiquidation(string operationId, string instrument, decimal volume)
        {
            _threadSwitcher.SwitchThread(async () =>
            {
                try
                {
                    _chaosKitty.Meow($"FakeGetPriceForSpecialLiquidation : {operationId} : {instrument} : {volume}");
                    
                    var quote = _quoteCacheService.GetQuote(instrument);

                    var price = (volume > 0 ? quote.Ask : quote.Bid) * _specialLiquidationSettings.FakePriceMultiplier;
                
                    await Task.Delay(1000);//waiting for the state to be saved into the repo
                
                    _cqrsSender.PublishEvent(new PriceForSpecialLiquidationCalculatedEvent
                    {
                        OperationId = operationId,
                        CreationTime = _dateService.Now(),
                        Instrument = instrument,
                        Volume = volume,
                        Price = price,
                    }, _cqrsContextNamesSettings.Gavel);
                }
                catch (Exception e)
                {
                    _cqrsSender.PublishEvent(new PriceForSpecialLiquidationCalculationFailedEvent
                    {
                        OperationId = operationId,
                        CreationTime = _dateService.Now(),
                        Reason = e.Message
                    });
                }
            });
        }
    }
}