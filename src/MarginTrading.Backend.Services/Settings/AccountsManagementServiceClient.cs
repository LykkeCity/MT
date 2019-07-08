// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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