using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Services.Settings
{
    [UsedImplicitly]
    public class AccountsManagementServiceClient
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
		
        [Optional]
        public string ApiKey { get; set; }
    }
}