using Newtonsoft.Json;

namespace MarginTrading.Backend.Services.Notifications
{
    public class IosNotification
    {
        [JsonProperty("aps")]
        public IosPositionFields Aps { get; set; }
    }
}