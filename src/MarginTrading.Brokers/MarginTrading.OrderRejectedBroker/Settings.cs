using MarginTrading.BrokerBase.Settings;
using MarginTrading.Core.Settings;

namespace MarginTrading.OrderRejectedBroker
{
    public class Settings : DefaultBrokerSettings
    {
        public MarginTradingSettings MtBackend { get; set; }
    }
}