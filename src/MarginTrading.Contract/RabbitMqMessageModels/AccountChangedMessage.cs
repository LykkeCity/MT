// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Contract.BackendContracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Contract.RabbitMqMessageModels
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