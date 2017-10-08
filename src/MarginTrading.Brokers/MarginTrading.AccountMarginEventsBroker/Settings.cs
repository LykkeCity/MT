using MarginTrading.BrokerBase.Settings;
using MarginTrading.Core.Settings;

namespace MarginTrading.AccountMarginEventsBroker
{
    public class Settings : DefaultBrokerSettings
    {
        public MarginTradingSettings MtBackend { get; set; }
    }
}