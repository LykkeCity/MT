using System;
using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.PersonalData.Contract;

namespace MarginTrading.Common.Services.Client
{
    public class ClientAccountService : IClientAccountService
    {
        private readonly IClientAccountClient _clientAccountsClient;
        private readonly IPersonalDataService _personalDataService;

        public ClientAccountService(
            IClientAccountClient clientAccountsClient,
            IPersonalDataService personalDataService)
        {
            _clientAccountsClient = clientAccountsClient;
            _personalDataService = personalDataService;
        }

        public async Task<string> GetNotificationId(string clientId)
        {
            var clientAcc = await _clientAccountsClient.GetByIdAsync(clientId);

            if (clientAcc != null)
            {
                return clientAcc.NotificationsId;
            }

            throw new Exception($"Can't get notification Id for clientId = {clientId}");
        }

        public async Task<string> GetEmail(string clientId)
        {
            return await _personalDataService.GetEmailAsync(clientId);
        }

        public async Task<bool> IsPushEnabled(string clientId)
        {
            var pushSettings = await _clientAccountsClient.GetPushNotificationAsync(clientId);

            return pushSettings != null && pushSettings.Enabled;
        }
    }
}
