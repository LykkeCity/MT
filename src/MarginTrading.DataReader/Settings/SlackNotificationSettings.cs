using Lykke.AzureQueueIntegration;

namespace MarginTrading.DataReader.Settings
{
    public class SlackNotificationSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }
    }
}