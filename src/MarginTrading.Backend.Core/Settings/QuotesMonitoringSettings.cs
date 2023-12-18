// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Settings
{
    public class QuotesMonitoringSettings
    {
        public bool IsEnabled { get; set; }
        public TimeSpan ConsiderQuoteStalePeriod { get; set; }
    }
}