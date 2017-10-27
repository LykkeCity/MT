using System;

namespace MarginTrading.MarketMaker.Models.Api
{
    public class OrderGenerationSettingsModel
    {
        public decimal VolumeMultiplier { get; set; }
        public TimeSpan OrderRenewalDelay { get; set; }
    }
}