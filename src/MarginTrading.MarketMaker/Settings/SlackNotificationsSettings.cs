using Lykke.AzureQueueIntegration;

namespace MarginTrading.MarketMaker.Settings
{
    public class SlackNotificationsSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }

        public int ThrottlingLimitSeconds { get; set; }
    }
}