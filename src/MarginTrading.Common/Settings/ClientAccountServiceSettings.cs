// Copyright (c) 2019 Lykke Corp.

using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Common.Settings
{
	[UsedImplicitly]
	public class ClientAccountServiceSettings
	{
		[HttpCheck("/api/isalive")]
		public string ServiceUrl { get; set; }
	}
}