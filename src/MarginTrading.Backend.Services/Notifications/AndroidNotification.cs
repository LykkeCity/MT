using Newtonsoft.Json;

namespace MarginTrading.Backend.Services.Notifications
{
    public class AndroidNotification
    {
        [JsonProperty("data")]
        public AndroidPositionFields Data { get; set; }
    }
}