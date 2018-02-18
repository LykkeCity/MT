using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Common.Settings
{
	public class ClientAccountServiceSettings
	{
		[HttpCheck("/api/isalive")]
		public string ServiceUrl { get; set; }
	}
}