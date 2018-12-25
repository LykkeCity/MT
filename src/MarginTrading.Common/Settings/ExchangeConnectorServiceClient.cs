using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Common.Settings
{
    [UsedImplicitly]
    public class ExchangeConnectorServiceClient
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }

        public string ApiKey { get; set; }
    }
}