using System.Threading.Tasks;
using Common;
using FluentAssertions;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
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
        private MarginSettings _marginSettings;

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
                ExchangeName = "exchange name"
            };
            var rabbitMqService = Mock.Of<IRabbitMqService>(s =>
                s.GetProducer(expectedRabbitMqSettings.Equivalent(), true,
                    s.GetJsonSerializer<MarginTradingEnabledChangedMessage>()) ==
                publisher);
            _marginSettings = new MarginSettings
            {
                MtRabbitMqConnString = "conn str",
                RabbitMqQueues = new RabbitMqQueues
                {
                    MarginTradingEnabledChanged = new RabbitMqQueueInfo {ExchangeName = "exchange name"}
                }
            };

            _marginTradingSettingsCacheService = Mock.Of<IMarginTradingSettingsCacheService>();
            _sut = new MarginTradingEnablingService(_clientAccountsService, rabbitMqService, _marginSettings,
                _marginTradingSettingsCacheService);
            _sut.Start();
        }

        [Test]
        public async Task Always_ShouldCorrectlyEnableDemo()
        {
            // arrange
            _marginSettings.IsLive = false;

            //act
            await _sut.SetMarginTradingEnabled("id of client", enabled: true);

            //assert
            Mock.Get(_clientAccountsService).Verify(r => r.SetMarginEnabledAsync("id of client", true, false, false));
            var expectedMessage = new MarginTradingEnabledChangedMessage
            {
                ClientId = "id of client",
                EnabledDemo = true,
                EnabledLive = false
            };
            _sentMessage.Should().BeEquivalentTo(expectedMessage);
            Mock.Get(_marginTradingSettingsCacheService)
                .Verify(s => s.OnMarginTradingEnabledChanged(expectedMessage.Equivalent()));
        }

        [Test]
        public async Task Always_ShouldCorrectlyEnableLive()
        {
            // arrange
            _marginSettings.IsLive = true;

            //act
            await _sut.SetMarginTradingEnabled("id of client", enabled: true);

            //assert
            Mock.Get(_clientAccountsService).Verify(r => r.SetMarginEnabledAsync("id of client", false, true, false));
            var expectedMessage = new MarginTradingEnabledChangedMessage
            {
                ClientId = "id of client",
                EnabledDemo = false,
                EnabledLive = true
            };
            _sentMessage.Should().BeEquivalentTo(expectedMessage);
            Mock.Get(_marginTradingSettingsCacheService)
                .Verify(s => s.OnMarginTradingEnabledChanged(expectedMessage.Equivalent()));
        }
    }
}