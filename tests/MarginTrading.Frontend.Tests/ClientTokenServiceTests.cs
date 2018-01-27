using System.Threading.Tasks;
using Autofac;
using MarginTrading.Frontend.Services;
using NUnit.Framework;

namespace MarginTrading.Frontend.Tests
{
    public class ClientTokenServiceTests : BaseTests
    {
        private IClientTokenService _clientTokenService;

        [OneTimeSetUp]
        public void SetUp()
        {
            RegisterDependencies();
            _clientTokenService = Container.Resolve<IClientTokenService>();
        }

        [Test]
        public async Task Is_ClientId_Returned()
        {
            var clientId = await _clientTokenService.GetClientId("test");
            Assert.AreEqual("1", clientId);

            clientId = await _clientTokenService.GetClientId("test1");
            Assert.AreEqual("1", clientId);
        }
    }
}
