// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ClientAccount.Client.Models;

namespace MarginTrading.Common.Services.Client
{
    public class ClientAccountService : IClientAccountService
    {
        private readonly IClientAccountClient _clientAccountsClient;

        public ClientAccountService(IClientAccountClient clientAccountsClient)
        {
            _clientAccountsClient = clientAccountsClient;
        }

        public async Task<string> GetNotificationId(string clientId)
        {
            var clientAcc = await _clientAccountsClient.GetByIdAsync(clientId);

            if (clientAcc != null)
            {
                return clientAcc.NotificationsId;
            }

            throw new Exception(string.Format("Can't get notification Id for clientId = {0}", clientId));
        }

        public async Task<string> GetEmail(string clientId)
        {
            var clientAcc = await _clientAccountsClient.GetByIdAsync(clientId);

            return clientAcc?.Email;
        }

        public async Task<bool> IsPushEnabled(string clientId)
        {
            var pushSettings = await _clientAccountsClient.GetPushNotificationAsync(clientId);

            return pushSettings != null && pushSettings.Enabled;
        }

        public Task<MarginEnabledSettingsModel> GetMarginEnabledAsync(string clientId)
        {
            return _clientAccountsClient.GetMarginEnabledAsync(clientId);
        }

        public Task SetMarginEnabledAsync(string clientId, bool settingsEnabled, bool settingsEnabledLive,
            bool settingsTermsOfUseAgreed)
        {
            return _clientAccountsClient.SetMarginEnabledAsync(clientId, settingsEnabled, settingsEnabledLive,
                settingsTermsOfUseAgreed);
        }
    }
}
