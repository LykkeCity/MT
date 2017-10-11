using Newtonsoft.Json;

namespace MarginTrading.Client.JsonResults
{
    class AuthResult
    {

        [JsonProperty("KycStatus")]
        public string KycStatus { get; set; }
        [JsonProperty("PinIsEntered ")]
        public bool PinIsEntered { get; set; }
        [JsonProperty("Token")]
        public string Token { get; set; }
        [JsonProperty("NotificationsId")]
        public string NotificationsId { get; set; }
    }
}
