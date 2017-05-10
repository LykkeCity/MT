using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Common;
using MarginTrading.Core;
using MarginTrading.Core.Notifications;
using MarginTrading.Services.Generated.ClientAccountServiceApi;
using MarginTrading.Services.Generated.ClientAccountServiceApi.Models;
using MarginTrading.Services.Generated.SessionServiceApi;
using MarginTrading.Services.Generated.SessionServiceApi.Models;
using Microsoft.Rest;
using Moq;
using WampSharp.V2.Realm;
using IAppNotifications = MarginTrading.Services.Notifications.IAppNotifications;

namespace MarginTradingTests.Modules
{
    public class MockBaseServicesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ThreadSwitcherMock>()
                .As<IThreadSwitcher>()
                .SingleInstance();

            var emailService = new Mock<IEmailService>();
            var appNotifications = new Mock<IAppNotifications>();
            var realm = new Mock<IWampHostedRealm>();
            realm.Setup(x => x.Services.GetSubject<OrderBookLevel>(It.IsAny<string>()))
                .Returns(new Subject<OrderBookLevel>());
            var notifyService = new Mock<IClientNotifyService>();
            var rabbitMqNotifyService = new Mock<IRabbitMqNotifyService>();
            var consoleWriterMock = new Mock<IConsole>();
            var slackNotificationsMock = new Mock<ISlackNotificationsProducer>();
            var sessionServiceMock = new Mock<ISessionService>();

            sessionServiceMock
                .Setup(item => item.ApiSessionGetPostWithHttpMessagesAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new HttpOperationResponse<ClientSessionModel> { Body = new ClientSessionModel{ClientId = "1"}}));

            var clientAccountsServiceMock = new Mock<IClientAccountService>();
            clientAccountsServiceMock
                .Setup(item => item.ApiClientAccountsGetByIdPostWithHttpMessagesAsync(It.IsAny<GetByIdRequest>(), null, It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new HttpOperationResponse<IClientAccount> { Body = new IClientAccount(null, null, null, null, null, null, Guid.NewGuid().ToString())}));

            builder.RegisterInstance(emailService.Object).As<IEmailService>();
            builder.RegisterInstance(appNotifications.Object).As<IAppNotifications>();
            builder.RegisterInstance(appNotifications).As<Mock<IAppNotifications>>();
            builder.RegisterInstance(realm.Object).As<IWampHostedRealm>();
            builder.RegisterInstance(notifyService.Object).As<IClientNotifyService>();
            builder.RegisterInstance(rabbitMqNotifyService.Object).As<IRabbitMqNotifyService>();
            builder.RegisterInstance(consoleWriterMock.Object).As<IConsole>();
            builder.RegisterInstance(slackNotificationsMock.Object).As<ISlackNotificationsProducer>();
            builder.RegisterInstance(sessionServiceMock.Object).As<ISessionService>();
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
