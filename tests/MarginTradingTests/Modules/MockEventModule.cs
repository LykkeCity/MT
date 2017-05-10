using Autofac;
using MarginTrading.Services.Events;
using Moq;

namespace MarginTradingTests.Modules
{
    public class MockEventModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(new Mock<IEventChannel<BestPriceChangeEventArgs>>().Object)
                .As<IEventChannel<BestPriceChangeEventArgs>>()
                .SingleInstance();

            builder.RegisterInstance(new Mock<IEventChannel<MarginCallEventArgs>>().Object)
                .As<IEventChannel<MarginCallEventArgs>>()
                .SingleInstance();

            builder.RegisterInstance(new Mock<IEventChannel<OrderBookChangeEventArgs>>().Object)
                .As<IEventChannel<OrderBookChangeEventArgs>>()
                .SingleInstance();

            builder.RegisterInstance(new Mock<IEventChannel<OrderCancelledEventArgs>>().Object)
                .As<IEventChannel<OrderCancelledEventArgs>>()
                .SingleInstance();

            builder.RegisterInstance(new Mock<IEventChannel<OrderPlacedEventArgs>>().Object)
                .As<IEventChannel<OrderPlacedEventArgs>>()
                .SingleInstance();

            builder.RegisterInstance(new Mock<IEventChannel<OrderClosedEventArgs>>().Object)
                .As<IEventChannel<OrderClosedEventArgs>>()
                .SingleInstance();

            builder.RegisterInstance(new Mock<IEventChannel<StopOutEventArgs>>().Object)
                .As<IEventChannel<StopOutEventArgs>>()
                .SingleInstance();

            builder.RegisterInstance(new Mock<IEventChannel<LimitOrderSetEventArgs>>().Object)
                .As<IEventChannel<LimitOrderSetEventArgs>>()
                .SingleInstance();
        }
    }
}