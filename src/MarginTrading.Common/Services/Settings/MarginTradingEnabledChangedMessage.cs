// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Common.Services.Settings
{
    public class MarginTradingEnabledChangedMessage
    {
        public string ClientId { set; get; }
        public bool EnabledDemo { get; set; }
        public bool EnabledLive { get; set; }
    }
}