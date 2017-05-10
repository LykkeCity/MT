using System.Threading.Tasks;
using MarginTrading.Core;
using MarginTrading.Services.Generated.SessionServiceApi;

namespace MarginTrading.Services
{
    public class ClientTokenService : IClientTokenService
    {
        private readonly ISessionService _sessionService;

        public ClientTokenService(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        public async Task<string> GetClientId(string token)
        {
            try
            {
                var sessionModel = await _sessionService.ApiSessionGetPostAsync(token);
                return sessionModel?.ClientId;
            }
            catch
            {
                return null;
            }
        }
    }
}
