// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Common.Services.Settings
{
    public class MarginTradingEnabledChangedMessage
    {
        public string ClientId { set; get; }
        public bool EnabledDemo { get; set; }
        public bool EnabledLive { get; set; }
    }
}