using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Core.Settings
{
    public class EmailSenderSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
    }
}