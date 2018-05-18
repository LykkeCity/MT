using System;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Common;
using Lykke.Cqrs;
using Lykke.Service.ClientAccount.Client.Models;
using Lykke.Service.Session.AutorestClient;
using Lykke.Service.Session.AutorestClient.Models;
using Microsoft.Rest;
using Moq;
using WampSharp.V2.Realm;
using IAppNotifications = MarginTrading.Backend.Services.Notifications.IAppNotifications;
using Lykke.Service.Session;
using Lykke.SlackNotifications;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Backend.Services.Stubs;
using MarginTrading.Backend.Services.Workflow;
using MarginTrading.Common.Services;
using MarginTrading.Common.Services.Client;

namespace MarginTradingTests.Modules
{
    public class MockBaseServicesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ThreadSwitcherMock>().As<IThreadSwitcher>().SingleInstance();

            var emailService = new Mock<IEmailService>();
            var appNotifications = new Mock<IAppNotifications>();
            var realm = new Mock<IWampHostedRealm>();
            realm.Setup(x => x.Services.GetSubject<OrderBookLevel>(It.IsAny<string>()))
                .Returns(new Subject<OrderBookLevel>());
            var notifyService = new Mock<IClientNotifyService>();
            var rabbitMqNotifyService = new Mock<IRabbitMqNotifyService>();
            var consoleWriterMock = new Mock<IConsole>();
            var sessionServiceMock = new Mock<ISessionService>();
            var slackNotificationsMock = new Mock<ISlackNotificationsSender>();

            sessionServiceMock
                .Setup(item => item.ApiSessionGetPostWithHttpMessagesAsync(It.IsAny<ClientSessionGetRequest>(), null,
                    It.IsAny<CancellationToken>())).Returns(() => Task.FromResult(
                    new HttpOperationResponse<ClientSessionGetResponse>
                    {
                        Body = new ClientSessionGetResponse {Session = new ClientSessionModel {ClientId = "1"}}
                    }));

            var clientsRepositoryMock = new Mock<IClientsSessionsRepository>();
            clientsRepositoryMock.Setup(item => item.GetAsync(It.IsAny<string>())).Returns(() =>
                Task.FromResult((IClientSession) new ClientSession {ClientId = "1"}));

            var volumeEquivalentService = new Mock<IEquivalentPricesService>();

            var clientAccountMock = new Mock<IClientAccountService>();
            clientAccountMock.Setup(s => s.GetEmail(It.IsAny<string>())).ReturnsAsync("email@email.com");
            clientAccountMock.Setup(s => s.GetMarginEnabledAsync(It.IsAny<string>())).ReturnsAsync(
                new MarginEnabledSettingsModel() {Enabled = true, EnabledLive = true, TermsOfUseAgreed = true});
            clientAccountMock.Setup(s => s.GetNotificationId(It.IsAny<string>())).ReturnsAsync("notificationId");
            clientAccountMock.Setup(s => s.IsPushEnabled(It.IsAny<string>())).ReturnsAsync(true);

            builder.RegisterInstance(emailService.Object).As<IEmailService>();
            builder.RegisterInstance(appNotifications.Object).As<IAppNotifications>();
            builder.RegisterInstance(appNotifications).As<Mock<IAppNotifications>>();
            builder.RegisterInstance(realm.Object).As<IWampHostedRealm>();
            builder.RegisterInstance(notifyService.Object).As<IClientNotifyService>();
            builder.RegisterInstance(rabbitMqNotifyService.Object).As<IRabbitMqNotifyService>();
            builder.RegisterInstance(consoleWriterMock.Object).As<IConsole>();
            builder.RegisterInstance(clientsRepositoryMock.Object).As<IClientsSessionsRepository>();
            builder.RegisterInstance(sessionServiceMock.Object).As<ISessionService>();
            builder.RegisterInstance(slackNotificationsMock.Object).As<ISlackNotificationsSender>();
            builder.RegisterInstance(volumeEquivalentService.Object).As<IEquivalentPricesService>();
            builder.RegisterInstance(clientAccountMock.Object).As<IClientAccountService>();

            builder.RegisterType<DateService>().As<IDateService>().SingleInstance();
            builder.RegisterInstance(new Mock<ICqrsEngine>(MockBehavior.Loose).Object).As<ICqrsEngine>()
                .SingleInstance();
            builder.RegisterInstance(new CqrsContextNamesSettings()).AsSelf().SingleInstance();
            builder.RegisterType<AccountsProjection>().AsSelf().SingleInstance();
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