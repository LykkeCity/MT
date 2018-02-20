using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Lykke.Common;
using Lykke.Service.Session;
using MarginTrading.Common.Services.Client;
using MarginTrading.Common.Services.Settings;
using MarginTrading.Frontend.Services;
using Moq;

namespace MarginTrading.Frontend.Tests.Modules
{
    public class MockBaseServicesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ThreadSwitcherMock>()
                .As<IThreadSwitcher>()
                .SingleInstance();

            var httpRequestServiceMock = new Mock<IHttpRequestService>();
            httpRequestServiceMock
                .Setup(item => item.RequestIfAvailableAsync(It.IsAny<object>(), "init.availableassets", It.IsAny<Func<List<string>>>(), It.IsAny<EnabledMarginTradingTypes>(), "mt"))
                .Returns(() => Task.FromResult((new List<string> { "EURUSD" }, new List<string> { "BTCCHF" })));

            var clientsRepositoryMock = new Mock<IClientsSessionsRepository>();
            clientsRepositoryMock
                .Setup(item => item.GetAsync(It.IsAny<string>()))
                .Returns(() => Task.FromResult((IClientSession)new ClientSession { ClientId = "1" }));

            var clientAccountsServiceMock = new Mock<IClientAccountService>();
            clientAccountsServiceMock
                .Setup(item => item.GetNotificationId(It.IsAny<string>()))
                .Returns(() => Task.FromResult(Guid.NewGuid().ToString()));
            
            builder.RegisterInstance(httpRequestServiceMock.Object).As<IHttpRequestService>();
            builder.RegisterInstance(clientsRepositoryMock.Object).As<IClientsSessionsRepository>();
            builder.RegisterInstance(clientAccountsServiceMock.Object).As<IClientAccountService>();
        }
    }

    public class ThreadSwitcherMock : IThreadSwitcher
    {
        public void SwitchThread(Func<Task> taskProc)
        {
            taskProc().Wait();
        }
    }
}
