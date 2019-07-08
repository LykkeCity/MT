// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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