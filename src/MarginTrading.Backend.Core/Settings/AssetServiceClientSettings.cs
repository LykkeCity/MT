using System;

namespace MarginTrading.Backend.Core.Settings
{
    public class AssetClientSettings
    {
        public string ServiceUrl { get; set; }
        public TimeSpan CacheExpirationPeriod { get; set; }
    }
}