using Lykke.AzureQueueIntegration;

namespace MarginTrading.Services.Settings
{
    public class SlackNotificationSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }
    }
}
