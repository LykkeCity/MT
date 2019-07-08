// Copyright (c) 2019 Lykke Corp.

using Lykke.SettingsReader.Attributes;

namespace MarginTrading.BrokerBase.Settings
{
    public class BrokerSettingsBase
    {
        public string MtRabbitMqConnString { get; set; }
        [Optional]
        public bool IsLive { get; set; }
        [Optional]
        public string Env { get; set; }
    }
}