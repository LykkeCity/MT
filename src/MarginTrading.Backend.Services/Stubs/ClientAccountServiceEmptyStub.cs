using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.ClientAccount.Client.Models;
using MarginTrading.Common.Services.Client;

namespace MarginTrading.Backend.Services.Stubs
{
    [UsedImplicitly]
    public class ClientAccountServiceEmptyStub : IClientAccountService
    {
        public Task<string> GetNotificationId(string clientId)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<string> GetEmail(string clientId)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<bool> IsPushEnabled(string clientId)
        {
            return Task.FromResult(false);
        }

        public Task<MarginEnabledSettingsModel> GetMarginEnabledAsync(string clientId)
        {
            return Task.FromResult(new MarginEnabledSettingsModel
            {
                Enabled = true,
                EnabledLive = true,
                TermsOfUseAgreed = true
            });
        }

        public Task SetMarginEnabledAsync(string clientId, bool settingsEnabled, bool settingsEnabledLive,
            bool settingsTermsOfUseAgreed)
        {
            return Task.CompletedTask;
        }
    }
}