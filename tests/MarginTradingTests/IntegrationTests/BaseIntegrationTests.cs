using System;
using Autofac;
using MarginTrading.Services.Generated.ClientAccountServiceApi;
using MarginTrading.Services.Generated.SessionServiceApi;
using MarginTradingTests.IntegrationTests.Client;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace MarginTradingTests.IntegrationTests
{
    public class BaseIntegrationTests
    {
        protected IContainer Container { get; set; }

        protected void RegisterDependencies()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(TestContext.CurrentContext.TestDirectory)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile("appsettings.dev.json", true, true)
                .Build();

            var builder = new ContainerBuilder();

            builder.Register<ISessionService>(ctx =>
                new SessionService(new Uri(configuration["SessionServiceApiUrl"]))
            ).SingleInstance();

            builder.Register<IClientAccountService>(ctx =>
                new ClientAccountService(new Uri(configuration["ClientAccountServiceApiUrl"]))
            ).SingleInstance();

            builder.RegisterType<MtClient>()
                .AsSelf()
                .SingleInstance();

            Container = builder.Build();
        }
    }
}
