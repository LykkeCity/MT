// Copyright (c) 2019 Lykke Corp.

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
        public decimal FakePrice { get; set; } = 10;

        [Optional]
        public int PriceRequestTimeoutSec { get; set; } = 3600;

        [Optional]
        public TimeSpan RetryTimeout { get; set; } = new TimeSpan(0, 1, 0);
    }
}