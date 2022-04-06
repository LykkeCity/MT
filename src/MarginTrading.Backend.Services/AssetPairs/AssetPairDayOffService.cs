// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core;

// ReSharper disable PossibleInvalidOperationException

namespace MarginTrading.Backend.Services.AssetPairs
{
    public class AssetPairDayOffService : IAssetPairDayOffService
    {
        private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;

        public AssetPairDayOffService(IScheduleSettingsCacheService scheduleSettingsCacheService)
        {
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
        }

        public InstrumentTradingStatus IsAssetTradingDisabled(string assetPairId)
        {
            return _scheduleSettingsCacheService.GetInstrumentTradingStatus(assetPairId, TimeSpan.Zero);
        }
        
        public bool ArePendingOrdersDisabled(string assetPairId)
        {
            //TODO TBD in https://lykke-snow.atlassian.net/browse/MTC-155
            return false; //IsNowNotInSchedule(assetPairId, _dayOffSettingsService.GetScheduleSettings().PendingOrdersCutOff);
        }
    }
}
