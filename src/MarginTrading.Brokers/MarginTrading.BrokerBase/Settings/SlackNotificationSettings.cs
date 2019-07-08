// Copyright (c) 2019 Lykke Corp.

using Lykke.AzureQueueIntegration;

namespace MarginTrading.BrokerBase.Settings
{
    public class SlackNotificationSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }
    }
}
