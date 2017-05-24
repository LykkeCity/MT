using System.Threading.Tasks;
using Lykke.Service.Session;
using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class ClientTokenService : IClientTokenService
    {
        private readonly IClientsSessionsRepository _sessionService;

        public ClientTokenService(IClientsSessionsRepository sessionService)
        {
            _sessionService = sessionService;
        }

        public async Task<string> GetClientId(string token)
        {
            try
            {
                var sessionModel = await _sessionService.GetAsync(token);
                return sessionModel?.ClientId;
            }
            catch
            {
                return null;
            }
        }
    }
}
