using Autofac;

namespace MarginTrading.Services.Helpers
{
    public static class MarginTradingHelpers
    {
        public static object TradingMatchingSync = new object();

        #region BuildContainer
        public static IContainer BuildContainer()
        {
            var builder = new ContainerBuilder();

            RegisterCaches(builder);
            RegisterApplicationServices(builder);
            RegisterCommunicators(builder);

            return builder.Build();
        }
        #endregion

        private static void RegisterCaches(ContainerBuilder builder)
        {
            builder.RegisterType<OrdersCache>().AsSelf().SingleInstance();
            builder.RegisterType<InstrumentsCache>().AsSelf().SingleInstance();
        }

        private static void RegisterApplicationServices(ContainerBuilder builder)
        {
        }

        private static void RegisterCommunicators(ContainerBuilder builder)
        {
            // TODO: Implement communicators instead of injecting APP services into each other
        }
    }
}
