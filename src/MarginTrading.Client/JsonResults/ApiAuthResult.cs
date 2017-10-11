using Newtonsoft.Json;

namespace MarginTrading.Client.JsonResults
{
    class ApiAuthResult
    {
        [JsonProperty("Result")]
        public AuthResult Result { get; set; }
        [JsonProperty("Error")]
        public AuthError Error { get; set; }
    }
}
