namespace MarginTrading.Common.Settings
{
    public class SlackNotificationSettings
    {
        public AzureQueueSettings AzureQueue { get; set; }
    }
    
    public class AzureQueueSettings
    {
        public string ConnectionString { get; set; }

        public string QueueName { get; set; }
    }
}
