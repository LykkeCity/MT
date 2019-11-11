// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class BackendMaintenanceModeService : IMaintenanceModeService
    {
        private readonly MarginTradingSettings _settings;

        public BackendMaintenanceModeService(MarginTradingSettings settings)
        {
            _settings = settings;
        }
        
        private static bool IsEnabled { get; set; }

        public bool CheckIsEnabled()
        {
            return IsEnabled;
        }

        public void SetMode(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }
    }
}