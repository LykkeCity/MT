using JetBrains.Annotations;
using Lykke.Service.ExchangeConnector.Client;
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
        
        public ExchangeConnectorServiceSettings MtStpExchangeConnectorClient { get; set; }
        
        public SettingsServiceClient SettingsServiceClient { get; set; }
        
        public AccountsManagementServiceClient AccountsManagementServiceClient { get; set; }
    }
}