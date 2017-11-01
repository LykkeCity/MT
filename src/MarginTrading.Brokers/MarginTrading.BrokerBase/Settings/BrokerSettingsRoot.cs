﻿
namespace MarginTrading.BrokerBase.Settings
{
    public class BrokerSettingsRoot<TBrokerSettings>
        where TBrokerSettings: BrokerSettingsBase
    {
        public TBrokerSettings MarginTradingLive { get; set; }
        public TBrokerSettings MarginTradingDemo { get; set; }
    }
}