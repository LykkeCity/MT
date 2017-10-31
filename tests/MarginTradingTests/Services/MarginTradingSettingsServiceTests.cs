using System.Threading.Tasks;
using MarginTrading.Common.Settings;
using MarginTrading.Common.Settings.Models;
using MarginTrading.Common.Settings.Repositories;
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
            var clientSettingsRepository = Mock.Of<IClientSettingsRepository>(r => r.GetSettings<MarginEnabledSettings>("id of client") == Task.FromResult(new MarginEnabledSettings()));
            var appGlobalSettingsRepository = Mock.Of<IAppGlobalSettingsRepositry>();
            var sut = new MarginTradingSettingsService(clientSettingsRepository, appGlobalSettingsRepository, dummyCacheProvider);

            //act
            await sut.SetMarginTradingEnabled("id of client", isLive: false, enabled: true);

            //assert
            Mock.Get(clientSettingsRepository).Verify(r => r.SetSettings("id of client", It.Is<MarginEnabledSettings>(s => s.Enabled == true)));
            (await sut.IsMarginTradingEnabled("id of client")).Should().Match(t => t.Demo == true);
        }


        [Test]
        public async Task Always_ShouldCorrectlyEnableLive()
        {
            //arrange

            var dummyCacheProvider = new DummyCacheProvider();
            dummyCacheProvider.Add("{MarginTradingSettingsService}{IsMarginTradingEnabledGlobally}", true, null);
            var clientSettingsRepository = Mock.Of<IClientSettingsRepository>(r => r.GetSettings<MarginEnabledSettings>("id of client") == Task.FromResult(new MarginEnabledSettings()));
            var appGlobalSettingsRepository = Mock.Of<IAppGlobalSettingsRepositry>();
            var sut = new MarginTradingSettingsService(clientSettingsRepository, appGlobalSettingsRepository, dummyCacheProvider);

            //act
            await sut.SetMarginTradingEnabled("id of client", isLive: true, enabled: true);

            //assert
            Mock.Get(clientSettingsRepository).Verify(r => r.SetSettings("id of client", It.Is<MarginEnabledSettings>(s => s.EnabledLive == true)));
            (await sut.IsMarginTradingEnabled("id of client")).Should().Match(t => t.Live == true);
        }
    }
}
