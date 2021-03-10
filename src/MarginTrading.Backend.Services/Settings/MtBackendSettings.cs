// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.Settings;

namespace MarginTrading.Backend.Services.Settings
{
    [UsedImplicitly]
    public class MtBackendSettings
    {
        public MarginTradingSettings MtBackend { get; set; }
        
        [Optional, CanBeNull]
        public EmailSenderSettings EmailSender { get; set; }
        
        [Optional, CanBeNull]
        public NotificationSettings Jobs { get; set; }
        
        [Optional, CanBeNull]
        public SlackNotificationSettings SlackNotifications { get; set; }
        
        [Optional, CanBeNull]
        public RiskInformingSettings RiskInformingSettings { get; set; }
        
        [Optional, CanBeNull]
        public ClientAccountServiceSettings ClientAccountServiceClient { get; set; }
        
        public ExchangeConnectorServiceClient MtStpExchangeConnectorClient { get; set; }
        
        public SettingsServiceClient SettingsServiceClient { get; set; }
        
        public AccountsManagementServiceClient AccountsManagementServiceClient { get; set; }
        
        public ServiceClientSettings OrderBookServiceClient { get; set; }
        public ServiceClientSettings MdmServiceClient { get; set; }
    }
}