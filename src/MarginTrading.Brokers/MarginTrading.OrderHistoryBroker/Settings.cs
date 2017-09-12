using MarginTrading.BrokerBase.Settings;
using MarginTrading.Core.Settings;

namespace MarginTrading.OrderHistoryBroker
{
    public class Settings : DefaultBrokerSettings
    {
        public MarginTradingSettings MtBackend { get; set; }
    }
}