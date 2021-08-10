using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.AssetService.Contracts.Enums;
using MarginTrading.AssetService.Contracts.MarketSettings;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.AssetPairs;

namespace MarginTrading.Backend.Services.Workflow
{
    public class MarketSettingsChangedProjection
    {
        private readonly IScheduleSettingsCacheService _scheduleSettingsCache;
        private readonly IOvernightMarginService _overnightMarginService;
        private readonly IScheduleControlService _scheduleControlService;

        public MarketSettingsChangedProjection(
            IScheduleSettingsCacheService scheduleSettingsCache,
            IOvernightMarginService overnightMarginService,
            IScheduleControlService scheduleControlService)
        {
            _scheduleSettingsCache = scheduleSettingsCache;
            _overnightMarginService = overnightMarginService;
            _scheduleControlService = scheduleControlService;
        }

        [UsedImplicitly]
        public async Task Handle(MarketSettingsChangedEvent e)
        {
            switch (e.ChangeType)
            {
                case ChangeType.Creation:
                case ChangeType.Edition:
                    if ( e.OldMarketSettings == null || IsScheduleDataChanged(e.OldMarketSettings, e.NewMarketSettings))
                    {
                        await _scheduleSettingsCache.UpdateAllSettingsAsync();
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

        private static bool IsScheduleDataChanged(MarketSettingsContract oldSettings, MarketSettingsContract newSettings)
        {
            var isSameSchedule = oldSettings.Open.SequenceEqual(newSettings.Open) &&
                                 oldSettings.Close.SequenceEqual(newSettings.Close) &&
                                 oldSettings.Holidays.SequenceEqual(newSettings.Holidays) &&
                                 oldSettings.Timezone == newSettings.Timezone &&
                                 oldSettings.MarketSchedule.HalfWorkingDays.SequenceEqual(newSettings.MarketSchedule.HalfWorkingDays);

            return !isSameSchedule;
        }
    }
}