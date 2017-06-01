using System;
using System.Threading.Tasks;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Core.Messages;

namespace MarginTrading.Services
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

            throw new Exception(string.Format(MtMessages.NotificationIdNotFound, clientId));
        }

        public async Task<IClientAccount> GetAsync(string clientId)
        {
            return await _clientAccountsRepository.GetByIdAsync(clientId);
        }
    }
}
