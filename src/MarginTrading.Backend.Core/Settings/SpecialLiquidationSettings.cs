using System;
using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Core.Settings
{
    [UsedImplicitly]
    public class SpecialLiquidationSettings
    {
        public bool Enabled { get; set; }
     
        /// <summary>
        /// If net position volume in <see cref="VolumeThresholdCurrency"/> is greater then specified value,
        /// special liquidation should be performed instead of ordinary
        /// </summary>
        [Optional]
        public decimal VolumeThreshold { get; set; }

        /// <summary>
        /// <see cref="VolumeThreshold"/> currency.
        /// Default value = "EUR"
        /// </summary>
        [Optional] 
        public string VolumeThresholdCurrency { get; set; } = "EUR";

        [Optional]
        public decimal FakePrice { get; set; } = 10;

        [Optional]
        public int PriceRequestTimeoutSec { get; set; } = 3600;

        [Optional]
        public TimeSpan RetryTimeout { get; set; } = new TimeSpan(0, 1, 0);
    }
}