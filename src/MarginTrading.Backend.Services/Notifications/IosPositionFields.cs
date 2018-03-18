using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Core.Notifications;
using MarginTrading.Contract.BackendContracts;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Services.Notifications
{
    public class IosPositionFields
    {
        [JsonProperty("alert")]
        public string Alert { get; set; }
        
        [JsonProperty("type")]
        public NotificationType Type { get; set; }
        
        [JsonProperty("order")]
        public OrderHistoryBackendContract Order { get; set; }
    }
}