using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Core.Settings
{
    [UsedImplicitly]
    public class ServiceClientSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
		
        [Optional]
        public string ApiKey { get; set; }
    }
}