using System;
using System.Collections.Generic;
using System.Text;

namespace MarginTrading.MarketMaker.Settings
{
    public class RabbitMqConsumersSettings
    {
        public RabbitConnectionSettings FiatOrderbooks { get; set; }
        public RabbitConnectionSettings CryptoOrderbooks { get; set; }
        public RabbitConnectionSettings SpotOrderBook { get; set; }
    }
}
