using System.Threading.Tasks;
using Common;
using FluentAssertions;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using MarginTrading.Backend.Contracts.RabbitMqMessages;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services.Settings;
using MarginTradingTests.Helpers;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests.Services
{
    [TestFixture]
    class MarginTradingEnablingServiceTests
    {
        private MarginTradingEnablingService _sut;
        private IClientAccountClient _clientAccountsService;
        private MarginTradingEnabledChangedMessage _sentMessage = null;
        private IMarginTradingSettingsCacheService _marginTradingSettingsCacheService;

        [SetUp]
        public void SetUp()
        {
            _clientAccountsService = Mock.Of<IClientAccountClient>(r =>
                r.GetMarginEnabledAsync("id of client") == Task.FromResult(new MarginEnabledSettingsModel()));
            var publisher = Mock.Of<IMessageProducer<MarginTradingEnabledChangedMessage>>();
            Mock.Get(publisher)
                .Setup(s => s.ProduceAsync(It.IsNotNull<MarginTradingEnabledChangedMessage>()))
                .Returns(Task.CompletedTask).Callback<MarginTradingEnabledChangedMessage>(m => _sentMessage = m);
            var expectedRabbitMqSettings = new RabbitMqSettings
            {
                ConnectionString = "conn str",
                ExchangeName = "exchange name",
                IsDurable = false
            };
            var rabbitMqService = Mock.Of<IRabbitMqService>(s =>
                s.CreateProducer<MarginTradingEnabledChangedMessage>(expectedRabbitMqSettings.Equivalent()) ==
                publisher);
            var marginSettings = new MarginSettings
            {
                MtRabbitMqConnString = "conn str",
                RabbitMqQueues = new RabbitMqQueues
                {
                    MarginTradingEnabledChanged = new RabbitMqQueueInfo {ExchangeName = "exchange name"}
                }
            };

            _marginTradingSettingsCacheService = Mock.Of<IMarginTradingSettingsCacheService>();
            _sut = new MarginTradingEnablingService(_clientAccountsService, rabbitMqService, marginSettings,
                _marginTradingSettingsCacheService);
            _sut.Start();
        }

        [Test]
        public async Task Always_ShouldCorrectlyEnableDemo()
        {
            //act
            await _sut.SetMarginTradingEnabled("id of client", isLive: false, enabled: true);

            //assert
            Mock.Get(_clientAccountsService).Verify(r => r.SetMarginEnabledAsync("id of client", true, false, false));
            var expectedMessage = new MarginTradingEnabledChangedMessage
            {
                ClientId = "id of client",
                EnabledDemo = true,
                EnabledLive = false
            };
            _sentMessage.ShouldBeEquivalentTo(expectedMessage);
            Mock.Get(_marginTradingSettingsCacheService).Verify(s => s.OnMarginTradingEnabledChanged(expectedMessage.Equivalent()));
        }

        [Test]
        public async Task Always_ShouldCorrectlyEnableLive()
        {
            //act
            await _sut.SetMarginTradingEnabled("id of client", isLive: true, enabled: true);

            //assert
            Mock.Get(_clientAccountsService).Verify(r => r.SetMarginEnabledAsync("id of client", false, true, false));
            var expectedMessage = new MarginTradingEnabledChangedMessage
            {
                ClientId = "id of client",
                EnabledDemo = false,
                EnabledLive = true
            };
            _sentMessage.ShouldBeEquivalentTo(expectedMessage);
            Mock.Get(_marginTradingSettingsCacheService).Verify(s => s.OnMarginTradingEnabledChanged(expectedMessage.Equivalent()));
        }
    }
}