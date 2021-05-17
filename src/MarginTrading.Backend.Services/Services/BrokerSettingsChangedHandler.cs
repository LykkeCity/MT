// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Snow.Mdm.Contracts.Models.Contracts;
using Lykke.Snow.Mdm.Contracts.Models.Events;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;

namespace MarginTrading.Backend.Services.Services
{
    public class BrokerSettingsChangedHandler
    {
        private readonly MarginTradingSettings _settings;
        private readonly IScheduleSettingsCacheService _scheduleSettingsCache;
        private readonly IOvernightMarginService _overnightMarginService;
        private readonly IScheduleControlService _scheduleControlService;

        public BrokerSettingsChangedHandler(
            MarginTradingSettings settings,
            IScheduleSettingsCacheService scheduleSettingsCache,
            IOvernightMarginService overnightMarginService,
            IScheduleControlService scheduleControlService)
        {
            _settings = settings;
            _scheduleSettingsCache = scheduleSettingsCache;
            _overnightMarginService = overnightMarginService;
            _scheduleControlService = scheduleControlService;
        }

        [UsedImplicitly]
        public async Task Handle(BrokerSettingsChangedEvent e)
        {
            switch (e.ChangeType)
            {
                case ChangeType.Creation:
                    break;
                case ChangeType.Edition:
                    if (e.OldValue == null || IsScheduleDataChanged(e.OldValue, e.NewValue, _settings.BrokerId))
                    {
                        await _scheduleSettingsCache.UpdateAllSettingsAsync(true);
                        _overnightMarginService.ScheduleNext();
                        _scheduleControlService.ScheduleNext();
                    }
                    break;
                case ChangeType.Deletion:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static bool IsScheduleDataChanged(BrokerSettingsContract oldSettings, BrokerSettingsContract newSettings, string brokerId)
        {
            var brokerConditionGuard = newSettings.BrokerId.Equals(brokerId, StringComparison.InvariantCultureIgnoreCase);
            if (!brokerConditionGuard) return false;

            var isSameSchedule = oldSettings.Open == newSettings.Open &&
                                 oldSettings.Close == newSettings.Close &&
                                 oldSettings.Timezone == newSettings.Timezone &&
                                 oldSettings.Holidays.SequenceEqual(newSettings.Holidays) &&
                                 oldSettings.Weekends.SequenceEqual(newSettings.Weekends) &&
                                 oldSettings.PlatformSchedule.HalfWorkingDays.SequenceEqual(newSettings.PlatformSchedule.HalfWorkingDays);

            return !isSameSchedule;
        }
    }
}