using System;
using Autofac;
using Lykke.Service.Session.AutorestClient;
using MarginTrading.Common.Services.Client;
using MarginTrading.Frontend.Tests.IntegrationTests.Client;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace MarginTrading.Frontend.Tests.IntegrationTests
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

            builder.RegisterType<ClientAccountService>()
                .As<IClientAccountService>()
                .SingleInstance();

            builder.RegisterType<MtClient>()
                .AsSelf()
                .SingleInstance();

            Container = builder.Build();
        }
    }
}
