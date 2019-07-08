// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.BrokerBase.Settings
{
    public class CurrentApplicationInfo
    {
        public CurrentApplicationInfo(bool isLive, string applicationVersion, string applicationName)
        {
            IsLive = isLive;
            ApplicationVersion = applicationVersion;
            ApplicationName = applicationName;
        }

        public bool IsLive { get; }
        public string ApplicationVersion { get; }
        public string ApplicationName { get; }

        public string ApplicationFullName => $"{ApplicationName}:{ApplicationVersion}";
    }
}