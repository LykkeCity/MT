// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;
using MarginTrading.Backend.Services.Events;
using Moq;

namespace MarginTradingTests.Modules
{
    public class MockEventModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(new Mock<EventChannel<BestPriceChangeEventArgs>>().Object)
                .As<IEventChannel<BestPriceChangeEventArgs>>()
                .SingleInstance();

            builder.RegisterInstance(new Mock<EventChannel<MarginCallEventArgs>>().Object)
                .As<IEventChannel<MarginCallEventArgs>>()
                .SingleInstance();

            builder.RegisterInstance(new Mock<EventChannel<OrderCancelledEventArgs>>().Object)
                .As<IEventChannel<OrderCancelledEventArgs>>()
                .SingleInstance();

            builder.RegisterInstance(new Mock<EventChannel<OrderPlacedEventArgs>>().Object)
                .As<IEventChannel<OrderPlacedEventArgs>>()
                .SingleInstance();

            builder.RegisterInstance(new Mock<EventChannel<OrderExecutedEventArgs>>().Object)
                .As<IEventChannel<OrderExecutedEventArgs>>()
                .SingleInstance();

            builder.RegisterInstance(new Mock<EventChannel<StopOutEventArgs>>().Object)
                .As<IEventChannel<StopOutEventArgs>>()
                .SingleInstance();
            
            builder.RegisterInstance(new Mock<EventChannel<PositionUpdateEventArgs>>().Object)
                .As<IEventChannel<PositionUpdateEventArgs>>()
                .SingleInstance();
            
            builder.RegisterInstance(new Mock<EventChannel<AccountBalanceChangedEventArgs>>().Object)
                .As<IEventChannel<AccountBalanceChangedEventArgs>>()
                .SingleInstance();
            
            builder.RegisterInstance(new Mock<EventChannel<OrderChangedEventArgs>>().Object)
                .As<IEventChannel<OrderChangedEventArgs>>()
                .SingleInstance();
            
            builder.RegisterInstance(new Mock<EventChannel<OrderExecutionStartedEventArgs>>().Object)
                .As<IEventChannel<OrderExecutionStartedEventArgs>>()
                .SingleInstance();

            builder.RegisterInstance(new Mock<EventChannel<OrderActivatedEventArgs>>().Object)
                .As<IEventChannel<OrderActivatedEventArgs>>()
                .SingleInstance();
            
            builder.RegisterInstance(new Mock<EventChannel<OrderRejectedEventArgs>>().Object)
                .As<IEventChannel<OrderRejectedEventArgs>>()
                .SingleInstance();
        }
    }
}