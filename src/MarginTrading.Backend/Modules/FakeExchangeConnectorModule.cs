using Autofac;
using Common.Log;
using MarginTrading.Backend.Core.FakeExchangeConnector;
using MarginTrading.Backend.Core.FakeExchangeConnector.Caches;
using MarginTrading.Backend.Services.FakeExchangeConnector;
using MarginTrading.Backend.Services.FakeExchangeConnector.Caches;

namespace MarginTrading.Backend.Modules
{
    public class FakeExchangeConnectorModule : Module
    {
        private readonly ILog _log;

        public FakeExchangeConnectorModule( ILog log)
        {
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ExchangeCache>()
                .As<IExchangeCache>()
                .SingleInstance();

            builder.RegisterType<FakeTradingService>()
                .As<IFakeTradingService>()
                .SingleInstance();
        }
    }
}