using System.Threading.Tasks;
using Autofac;
using MarginTradingTests.IntegrationTests.Client;
using NUnit.Framework;

namespace MarginTradingTests.IntegrationTests
{
    [TestFixture]
    [Ignore("Integration")]
    public class MtIntegrationTests : BaseIntegrationTests
    {
        private MtClient _client;

        [OneTimeSetUp]
        public void SetUp()
        {
            RegisterDependencies();
            _client = Container.Resolve<MtClient>();
            _client.Connect(TestEnv.Dev);
        }

        [Test]
        public async Task Check_AccountAssetPairs_In_Init_Data_Correct()
        {
            var initData = await _client.GetInitData();

            Assert.IsNotNull(initData);
            Assert.IsNotEmpty(initData.Demo.Accounts);
            Assert.IsNotEmpty(initData.Demo.TradingConditions);

            foreach (string key in initData.Demo.TradingConditions.Keys)
            {
                var assets = initData.Demo.TradingConditions[key];
                foreach (var asset in assets)
                {
                    Assert.That(asset.LeverageMaintenance > 0, () => $"wrong {nameof(asset.LeverageMaintenance)} for accountId = {key}, {asset.BaseAssetId}");
                    Assert.That(asset.LeverageInit > 0, () => $"wrong {nameof(asset.LeverageInit)} for {key}, {asset.BaseAssetId}");
                    Assert.That(asset.DeltaAsk > 0, () => $"wrong {nameof(asset.DeltaAsk)} for {key}, {asset.BaseAssetId}");
                    Assert.That(asset.DeltaBid > 0, () => $"wrong {nameof(asset.DeltaBid)} for {key}, {asset.BaseAssetId}");
                }
            }
        }
    }
}
