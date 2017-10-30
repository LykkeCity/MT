using System.Threading.Tasks;
using Autofac;
using Lykke.Service.Session.AutorestClient;
using MarginTrading.Backend.Core;
using MarginTrading.Frontend.Services;
using NUnit.Framework;

namespace MarginTradingTests
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
            string clientId = await _clientTokenService.GetClientId("test");
            Assert.AreEqual("1", clientId);

            clientId = await _clientTokenService.GetClientId("test1");
            Assert.AreEqual("1", clientId);
        }
    }
}
