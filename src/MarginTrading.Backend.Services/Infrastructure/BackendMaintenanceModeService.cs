// Copyright (c) 2019 Lykke Corp.

using Lykke.SlackNotifications;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.Enums;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class BackendMaintenanceModeService : IMaintenanceModeService
    {
        private readonly ISlackNotificationsSender _slackNotificationsSender;
        private readonly MarginTradingSettings _settings;

        public BackendMaintenanceModeService(ISlackNotificationsSender slackNotificationsSender,
            MarginTradingSettings settings)
        {
            _slackNotificationsSender = slackNotificationsSender;
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

            _slackNotificationsSender.SendAsync(ChannelTypes.Monitor, $"Backend {_settings.Env}",
                $"Maintenance mode is {(isEnabled ? "ENABLED" : "DISABLED")}");
        }
    }
}