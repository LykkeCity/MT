using System;
using System.Threading.Tasks;
using MarginTrading.Core;
using MarginTrading.Core.Messages;
using MarginTrading.Services.Generated.ClientAccountServiceApi;
using MarginTrading.Services.Generated.ClientAccountServiceApi.Models;

namespace MarginTrading.Services
{
    public class ClientNotificationService : IClientNotificationService
    {
        private readonly IClientAccountService _clientAccountService;

        public ClientNotificationService(IClientAccountService clientAccountService)
        {
            _clientAccountService = clientAccountService;
        }

        public async Task<string> GetNotificationId(string clientId)
        {
            var clientAcc = await _clientAccountService.ApiClientAccountsGetByIdPostAsync(new GetByIdRequest{ClientId = clientId});

            if (clientAcc != null)
            {
                return clientAcc.NotificationsId;
            }

            throw new Exception(string.Format(MtMessages.NotificationIdNotFound, clientId));
        }
    }
}
