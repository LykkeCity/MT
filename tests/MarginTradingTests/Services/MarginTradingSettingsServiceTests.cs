using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;
using MarginTrading.Common.Services.Settings;
using MarginTradingTests.Helpers;
using Moq;
using NUnit.Framework;
using Rocks.Caching;
// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable RedundantBoolCompare

namespace MarginTradingTests.Services
{
    [TestFixture]
    class MarginTradingSettingsServiceTests
    {
        [Test]
        public async Task Always_ShouldCorrectlyEnableDemo()
        {
            //arrange

            var dummyCacheProvider = new DummyCacheProvider();
            dummyCacheProvider.Add("{MarginTradingSettingsService}{IsMarginTradingEnabledGlobally}", true, null);
            var clientAccountsService = Mock.Of<IClientAccountClient>(r => r.GetMarginEnabledAsync("id of client") == Task.FromResult(new MarginEnabledSettingsModel()));
            var sut = new MarginTradingSettingsService(dummyCacheProvider, clientAccountsService);

            //act
            await sut.SetMarginTradingEnabled("id of client", isLive: false, enabled: true);

            //assert
            Mock.Get(clientAccountsService).Verify(r => r.SetMarginEnabledAsync("id of client", true, false, false));
            (await sut.IsMarginTradingEnabled("id of client")).Should().Match(t => t.Demo == true);
        }


        [Test]
        public async Task Always_ShouldCorrectlyEnableLive()
        {
            //arrange

            var dummyCacheProvider = new DummyCacheProvider();
            dummyCacheProvider.Add("{MarginTradingSettingsService}{IsMarginTradingEnabledGlobally}", true, null);
            var clientAccountsService = Mock.Of<IClientAccountClient>(r => r.GetMarginEnabledAsync("id of client") == Task.FromResult(new MarginEnabledSettingsModel()));
            var sut = new MarginTradingSettingsService(dummyCacheProvider, clientAccountsService);
            
            //act
            await sut.SetMarginTradingEnabled("id of client", isLive: true, enabled: true);

            //assert
            Mock.Get(clientAccountsService).Verify(r => r.SetMarginEnabledAsync("id of client", false, true, false));
            (await sut.IsMarginTradingEnabled("id of client")).Should().Match(t => t.Live == true);
        }
    }
}
