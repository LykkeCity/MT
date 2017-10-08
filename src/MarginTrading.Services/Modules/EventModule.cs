using Autofac;
using MarginTrading.Services.Events;

namespace MarginTrading.Services.Modules
{
    public class EventModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<EventChannel<BestPriceChangeEventArgs>>()
                .As<IEventChannel<BestPriceChangeEventArgs>>()
                .SingleInstance();

            builder.RegisterType<EventChannel<MarginCallEventArgs>>()
                .As<IEventChannel<MarginCallEventArgs>>()
                .SingleInstance();

            builder.RegisterType<EventChannel<OrderBookChangeEventArgs>>()
                .As<IEventChannel<OrderBookChangeEventArgs>>()
                .SingleInstance();

            builder.RegisterType<EventChannel<OrderCancelledEventArgs>>()
                .As<IEventChannel<OrderCancelledEventArgs>>()
                .SingleInstance();

            builder.RegisterType<EventChannel<OrderPlacedEventArgs>>()
                .As<IEventChannel<OrderPlacedEventArgs>>()
                .SingleInstance();

            builder.RegisterType<EventChannel<OrderClosedEventArgs>>()
                .As<IEventChannel<OrderClosedEventArgs>>()
                .SingleInstance();

            builder.RegisterType<EventChannel<StopOutEventArgs>>()
                .As<IEventChannel<StopOutEventArgs>>()
                .SingleInstance();

            builder.RegisterType<EventChannel<PositionUpdateEventArgs>>()
                .As<IEventChannel<PositionUpdateEventArgs>>()
                .SingleInstance();

            builder.RegisterType<EventChannel<AccountBalanceChangedEventArgs>>()
                .As<IEventChannel<AccountBalanceChangedEventArgs>>()
                .SingleInstance();
        }
    }
}