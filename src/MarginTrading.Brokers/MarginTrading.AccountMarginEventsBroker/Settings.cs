using MarginTrading.Backend.Core.Settings;
using MarginTrading.BrokerBase.Settings;

namespace MarginTrading.AccountMarginEventsBroker
{
    public class Settings : DefaultBrokerSettings
    {
        public MarginTradingSettings MtBackend { get; set; }
    }
}