using System.Threading.Tasks;
using MarginTrading.MarketMaker.Broker.Core.Services;
using Lykke.JobTriggers.Triggers.Attributes;
using MarginTrading.MarketMaker.Broker.Contract;

namespace MarginTrading.MarketMaker.Broker.TriggerHandlers
{
    // NOTE: This is the trigger handlers class example.
    // All triger's handlers are founded and added to container by JobTriggers infrastructure, 
    // when you call builder.AddTriggers() in Startup. Further, JobTriggers infrastructure manages handlers execution.
    public class MyHandlers
    {
        private readonly IMyFooService _myFooService;
        private readonly IMyBooService _myBooService;
        private readonly IHealthService _healthService;

        // NOTE: The object is instantiated using DI container, so registered dependencies are injects well
        public MyHandlers(IMyFooService myFooService, IMyBooService myBooService, IHealthService healthService)
        {
            _myFooService = myFooService;
            _myBooService = myBooService;
            _healthService = healthService;
        }

        [TimerTrigger("00:00:10")]
        public async Task TimeTriggeredHandler()
        {
            try
            {
                _healthService.TraceFooStarted();

                await _myFooService.FooAsync();

                _healthService.TraceFooCompleted();
            }
            catch
            {
                _healthService.TraceFooFailed();
            }
        }

        [QueueTrigger("queue-name")]
        public async Task QueueTriggeredHandler(MyMessage msg)
        {
            try
            {
                // TODO: See Foo
                //_healthService.TraceBooStarted();

                await _myBooService.BooAsync();

                // TODO: See Foo
                //_healthService.TraceBooCompleted();
            }
            catch
            {
                // TODO: See Foo
                //_healthService.TraceBooFailed();
            }
        }
    }
}