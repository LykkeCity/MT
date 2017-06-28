using System;
using System.Linq;
using MarginTrading.Core;
using MarginTrading.Core.Assets;
using MarginTrading.Core.Settings;

namespace MarginTrading.Services
{
    public class AssetDayOffService : IAssetDayOffService
    {
        private readonly IDateService _dateService;
        private readonly MarketMakerSettings _marketMakerSettings;

        public AssetDayOffService(IDateService dateService,
            MarketMakerSettings marketMakerSettings)
        {
            _dateService = dateService;
            _marketMakerSettings = marketMakerSettings;
        }

        public bool IsDayOff(string assetId)
        {
            if (_marketMakerSettings.AssetsWithoutDayOff?.Contains(assetId) == true)
                return false;

            var currentDateTime = _dateService.Now();
            var dayOfWeek = currentDateTime.DayOfWeek;
            var hour = currentDateTime.Hour;

            return ((dayOfWeek == _marketMakerSettings.DayOffStartDay && hour >= _marketMakerSettings.DayOffStartHour)
                    || GetDayOfWeekIndex(dayOfWeek) > GetDayOfWeekIndex(_marketMakerSettings.DayOffStartDay))
                   && ((dayOfWeek == _marketMakerSettings.DayOffEndDay && hour < _marketMakerSettings.DayOffEndHour)
                       || GetDayOfWeekIndex(dayOfWeek) < GetDayOfWeekIndex(_marketMakerSettings.DayOffEndDay));
        }

        private int GetDayOfWeekIndex(DayOfWeek dayOfWeek)
        {
            return dayOfWeek == DayOfWeek.Sunday ? 7 : (int) dayOfWeek;
        }
    }
}
