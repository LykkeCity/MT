using MarginTrading.Backend.Core.Settings;
using MarginTrading.BrokerBase.Settings;

namespace MarginTrading.OrderRejectedBroker
{
    public class Settings : DefaultBrokerSettings
    {
        public MarginTradingSettings MtBackend { get; set; }
    }
}