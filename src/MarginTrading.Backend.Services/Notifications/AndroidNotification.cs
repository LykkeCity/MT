// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;

namespace MarginTrading.Backend.Services.Notifications
{
    public class AndroidNotification
    {
        [JsonProperty("data")]
        public AndroidPositionFields Data { get; set; }
    }
}