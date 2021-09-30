// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Core.Settings
{
    [UsedImplicitly]
    public class StartupQueuesCheckerSettings
    {
        public string ConnectionString { get; set; }
        
        public string OrderHistoryQueueName { get; set; }
        
        public string PositionHistoryQueueName { get; set; }

        [Optional]
        public bool DisablePoisonQueueCheck { get; set; }
    }
}