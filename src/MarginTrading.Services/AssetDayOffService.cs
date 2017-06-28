using System;
using System.Linq;
using Common;
using MarginTrading.Core;
using MarginTrading.Core.Assets;
using MarginTrading.Core.Settings;

namespace MarginTrading.Services
{
    public class AssetDayOffService : IAssetDayOffService
    {
        private readonly IDateService _dateService;
        private readonly MarketMakerSettings _marketMakerSettings;
        private int _startDay;
        private int _endDay;

        public AssetDayOffService(IDateService dateService,
            MarketMakerSettings marketMakerSettings)
        {
            _dateService = dateService;
            _marketMakerSettings = marketMakerSettings;
            _startDay = GetDayOfWeekIndex(_marketMakerSettings.DayOffStartDay.ParseEnum<DayOfWeek>());
            _endDay = GetDayOfWeekIndex(_marketMakerSettings.DayOffEndDay.ParseEnum<DayOfWeek>());
        }

        public bool IsDayOff(string assetId)
        {
            if (_marketMakerSettings.AssetsWithoutDayOff?.Contains(assetId) == true)
                return false;

            var currentDateTime = _dateService.Now();
            int currentDayOfWeek = GetDayOfWeekIndex(currentDateTime.DayOfWeek);
            var currentHour = currentDateTime.Hour;

            return ((currentDayOfWeek == _startDay && currentHour >= _marketMakerSettings.DayOffStartHour)
                    || currentDayOfWeek > _startDay)
                   && ((currentDayOfWeek == _endDay && currentHour < _marketMakerSettings.DayOffEndHour)
                       || currentDayOfWeek < _endDay);
        }

        private int GetDayOfWeekIndex(DayOfWeek dayOfWeek)
        {
            return dayOfWeek == DayOfWeek.Sunday ? 7 : (int) dayOfWeek;
        }
    }
}
