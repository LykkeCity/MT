using Autofac;
using Autofac.Extensions.DependencyInjection;
using MarginTrading.Common.Helpers;
using MarginTrading.Common.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.Common.Modules
{
    public class MarginTradingCommonModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var services = new ServiceCollection();
            services.AddSingleton<ITelemetryInitializer, UserAgentTelemetryInitializer>();
            builder.Populate(services);

            builder.RegisterType<ConvertService>().As<IConvertService>().SingleInstance();
        }
    }
}