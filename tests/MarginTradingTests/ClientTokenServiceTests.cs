using System.Threading;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.AzureRepositories.Clients;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Services.Generated.SessionServiceApi;
using MarginTrading.Services.Generated.SessionServiceApi.Models;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    public class ClientTokenServiceTests : BaseTests
    {
        private IClientTokenService _clientTokenService;
        private ISessionService _sessionService;

        [OneTimeSetUp]
        public void SetUp()
        {
            RegisterDependencies();
            _clientTokenService = Container.Resolve<IClientTokenService>();
            _sessionService = Container.Resolve<ISessionService>();
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
