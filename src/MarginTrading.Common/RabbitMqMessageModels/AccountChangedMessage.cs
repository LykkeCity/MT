using MarginTrading.Common.BackendContracts;
using MarginTrading.Core.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Common.RabbitMqMessageModels
{
    /// <summary>
    /// Message about changed account
    /// </summary>
    public class AccountChangedMessage
    {
        /// <summary>
        /// Account snapshot at the moment of event
        /// </summary>
        public MarginTradingAccountBackendContract Account { get; set; }

        /// <summary>
        /// What happend to the account
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public AccountEventTypeEnum EventType { get; set; }
    }
}