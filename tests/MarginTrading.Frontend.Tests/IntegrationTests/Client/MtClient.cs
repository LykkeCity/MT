using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Service.Session.AutorestClient;
using Lykke.Service.Session.AutorestClient.Models;
using MarginTrading.Contract.ClientContracts;
using MarginTrading.Frontend.Wamp;
using WampSharp.V2;
using WampSharp.V2.Client;

namespace MarginTrading.Frontend.Tests.IntegrationTests.Client
{
    public class MtClient
    {
        private readonly ISessionService _sessionService;
        private string _token;
        private string _clientId;
        private string _serverAddress;
        private IWampRealmProxy _realmProxy;
        private IRpcMtFrontend _service;

        public MtClient(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public void Connect(TestEnv env)
        {
            SetEnv(env);
            var factory = new DefaultWampChannelFactory();
            var channel =
                factory.CreateJsonChannel(_serverAddress, "mtcrossbar");

            while (!channel.RealmProxy.Monitor.IsConnected)
            {
                try
                {
                    Console.WriteLine($"Trying to connect to server {_serverAddress}...");
                    channel.Open().Wait();
                }
                catch
                {
                    Console.WriteLine("Retrying in 5 sec...");
                    Thread.Sleep(5000);
                }
            }
            Console.WriteLine($"Connected to server {_serverAddress}");

            _realmProxy = channel.RealmProxy;
            _service = _realmProxy.Services.GetCalleeProxy<IRpcMtFrontend>();
        }

        public void SetEnv(TestEnv env)
        {
            switch (env)
            {
                case TestEnv.Local:
                    _clientId = "";
                    _serverAddress = "";
                    break;
                case TestEnv.Dev:
                    _clientId = "";
                    _serverAddress = "";
                    break;
                case TestEnv.Test:
                    _clientId = "";
                    _serverAddress = "";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var session = _sessionService.ApiSessionGetByClientPost(new ClientSessionGetByClientRequest(_clientId)).Sessions.FirstOrDefault();
            _token = session.SessionToken;
        }

        #region Rpc methods

        public async Task<InitDataLiveDemoClientResponse> GetInitData()
        {
            return await _service.InitData(_token);
        }

        #endregion
    }
}
