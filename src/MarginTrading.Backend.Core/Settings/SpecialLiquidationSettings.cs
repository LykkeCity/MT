// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Core.Settings
{
    [UsedImplicitly]
    public class SpecialLiquidationSettings
    {
        public bool Enabled { get; set; }

        [Optional]
        public decimal FakePriceMultiplier { get; set; } = 1;

        [Optional]
        public int PriceRequestTimeoutSec { get; set; } = 3600;

        [Optional]
        public TimeSpan? PriceRequestRetryTimeout { get; set; } = new TimeSpan(0, 1, 0);
        
        [Optional]
        public bool RetryPriceRequestForCorporateActions { get; set; } = false;
        
        [Optional]
        public TimeSpan PriceRequestTimeoutCheckPeriod { get; set; } = new TimeSpan(0, 1, 0);

        [Optional] 
        public bool FakePriceRequestAutoApproval { get; set; } = true;
    }
}