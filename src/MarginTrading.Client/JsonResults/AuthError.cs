using Newtonsoft.Json;

namespace MarginTrading.Client.JsonResults
{
    class AuthError
    {
        [JsonProperty("Code")]
        public int Code { get; set; }
        [JsonProperty("Field ")]
        public object Field { get; set; }
        [JsonProperty("Message")]
        public string Message { get; set; }
    }
}
