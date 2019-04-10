using System.Collections.Generic;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core.Settings
{
    [UsedImplicitly]
    public class StartupQueuesCheckerSettings
    {
        public string ConnectionString { get; set; }
        
        public string OrderHistoryQueueName { get; set; }
        
        public string PositionHistoryQueueName { get; set; }
    }
}