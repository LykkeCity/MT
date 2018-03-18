using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Contract.BackendContracts;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Services.Notifications
{
    public class AndroidPositionFields
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("entity")]
        public string Entity { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("order")]
        public OrderHistoryBackendContract Order { get; set; }
    }
}