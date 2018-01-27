using Autofac;
using Common.Log;
using MarginTrading.Frontend.Repositories.Contract;

namespace MarginTrading.Frontend.Tests.Modules
{
    public class MockRepositoriesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var watchListRepository = MarginTradingTestsUtils.GetPopulatedMarginTradingWatchListsRepository();

            builder.RegisterInstance(new LogToMemory()).As<ILog>();
            builder.RegisterInstance(watchListRepository).As<IMarginTradingWatchListRepository>().SingleInstance();
        }
    }
}