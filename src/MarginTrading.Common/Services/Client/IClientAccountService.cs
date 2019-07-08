// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Lykke.Service.ClientAccount.Client.Models;

namespace MarginTrading.Common.Services.Client
{
    public interface IClientAccountService
    {
        Task<string> GetNotificationId(string clientId);
        Task<string> GetEmail(string clientId);
        Task<bool> IsPushEnabled(string clientId);
        Task<MarginEnabledSettingsModel> GetMarginEnabledAsync(string clientId);
        Task SetMarginEnabledAsync(string clientId, bool settingsEnabled, bool settingsEnabledLive, bool settingsTermsOfUseAgreed);
    }
}
