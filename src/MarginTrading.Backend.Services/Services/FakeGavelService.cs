using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Services
{
    public class FakeGavelService : IFakeGavelService
    {
        private readonly ICqrsSender _cqrsSender;
        private readonly IDateService _dateService;
        private readonly SpecialLiquidationSettings _specialLiquidationSettings;

        public FakeGavelService(
            ICqrsSender cqrsSender,
            IDateService dateService,
            SpecialLiquidationSettings specialLiquidationSettings)
        {
            _cqrsSender = cqrsSender;
            _dateService = dateService;
            _specialLiquidationSettings = specialLiquidationSettings;
        }
        
        public void GetPriceForSpecialLiquidation(string operationId, string instrument, decimal volume)
        {
            _cqrsSender.PublishEvent(new PriceForSpecialLiquidationCalculatedEvent
            {
                OperationId = operationId,
                CreationTime = _dateService.Now(),
                Instrument = instrument,
                Volume = volume,
                Price = _specialLiquidationSettings.FakePrice,
            });
        }

        public void ExecuteSpecialLiquidationOrder(string operationId, string instrument, decimal volume,
            decimal price)
        {
            _cqrsSender.PublishEvent(new SpecialLiquidationOrderExecutedEvent
            {
                OperationId = operationId,
                CreationTime = _dateService.Now(),
            });
        }
    }
}