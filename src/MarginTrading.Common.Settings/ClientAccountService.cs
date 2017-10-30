using System;
using System.Threading.Tasks;
using MarginTrading.Common.Settings.Repositories;

namespace MarginTrading.Common.Settings
{
    public class ClientAccountService : IClientAccountService
    {
        private readonly IClientAccountsRepository _clientAccountsRepository;

        public ClientAccountService(IClientAccountsRepository clientAccountsRepository)
        {
            _clientAccountsRepository = clientAccountsRepository;
        }

        public async Task<string> GetNotificationId(string clientId)
        {
            var clientAcc = await _clientAccountsRepository.GetByIdAsync(clientId);

            if (clientAcc != null)
            {
                return clientAcc.NotificationsId;
            }

            throw new Exception(string.Format("Can't get notification Id for clientId = {0}", clientId));
        }

        public async Task<IClientAccount> GetAsync(string clientId)
        {
            return await _clientAccountsRepository.GetByIdAsync(clientId);
        }
    }
}
