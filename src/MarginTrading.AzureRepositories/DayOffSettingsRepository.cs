using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.DayOffSettings;

namespace MarginTrading.AzureRepositories
{
    internal class DayOffSettingsRepository : IDayOffSettingsRepository
    {
        private const string BlobContainer = "mt-dayoff-settings";
        private const string Key = "DayOffSettingsRoot";
        private readonly IMarginTradingBlobRepository _blobRepository;

        public DayOffSettingsRepository(IMarginTradingBlobRepository blobRepository)
        {
            _blobRepository = blobRepository;
        }

        public DayOffSettingsRoot Read()
        {
            return Convert(_blobRepository.Read<DayOffSettingsRootStorageModel>(BlobContainer, Key));
        }

        public void Write(DayOffSettingsRoot settings)
        {
            _blobRepository.Write(BlobContainer, Key, Convert(settings));
        }

        private static DayOffSettingsRootStorageModel Convert(DayOffSettingsRoot settingsRoot)
        {
            return new DayOffSettingsRootStorageModel
            {
                Exclusions = settingsRoot.Exclusions.ToImmutableDictionary(d => d.Key, d =>
                    new DayOffExclusionStorageModel
                    {
                        AssetPairRegex = d.Value.AssetPairRegex,
                        Id = d.Value.Id,
                        Start = d.Value.Start,
                        End = d.Value.End,
                        IsTradeEnabled = d.Value.IsTradeEnabled,
                    }),
                ScheduleSettings = new ScheduleSettingsStorageModel
                {
                    AssetPairsWithoutDayOff = settingsRoot.ScheduleSettings.AssetPairsWithoutDayOff,
                    DayOffEndDay = settingsRoot.ScheduleSettings.DayOffEndDay,
                    DayOffEndTime = settingsRoot.ScheduleSettings.DayOffEndTime,
                    DayOffStartDay = settingsRoot.ScheduleSettings.DayOffStartDay,
                    DayOffStartTime = settingsRoot.ScheduleSettings.DayOffStartTime,
                    PendingOrdersCutOff = settingsRoot.ScheduleSettings.PendingOrdersCutOff,
                }
            };
        }

        private static DayOffSettingsRoot Convert(DayOffSettingsRootStorageModel settings)
        {
            if (settings == null)
                return null;

            return new DayOffSettingsRoot(
                settings.Exclusions.ToImmutableDictionary(s => s.Key, s =>
                    new DayOffExclusion(
                        id: s.Value.Id,
                        assetPairRegex: s.Value.AssetPairRegex,
                        start: s.Value.Start,
                        end: s.Value.End,
                        isTradeEnabled: s.Value.IsTradeEnabled)),
                new ScheduleSettings(
                    dayOffStartDay: settings.ScheduleSettings.DayOffStartDay,
                    dayOffStartTime: settings.ScheduleSettings.DayOffStartTime,
                    dayOffEndDay: settings.ScheduleSettings.DayOffEndDay,
                    dayOffEndTime: settings.ScheduleSettings.DayOffEndTime,
                    assetPairsWithoutDayOff: settings.ScheduleSettings.AssetPairsWithoutDayOff,
                    pendingOrdersCutOff: settings.ScheduleSettings.PendingOrdersCutOff));
        }

        public class DayOffSettingsRootStorageModel
        {
            public ImmutableDictionary<Guid, DayOffExclusionStorageModel> Exclusions { get; set; }
            public ScheduleSettingsStorageModel ScheduleSettings { get; set; }
        }

        public class DayOffExclusionStorageModel
        {
            public Guid Id { get; set; }
            public string AssetPairRegex { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public bool IsTradeEnabled { get; set; }
        }

        public class ScheduleSettingsStorageModel
        {
            public DayOfWeek DayOffStartDay { get; set; }
            public TimeSpan DayOffStartTime { get; set; }
            public DayOfWeek DayOffEndDay { get; set; }
            public TimeSpan DayOffEndTime { get; set; }
            public HashSet<string> AssetPairsWithoutDayOff { get; set; }
            public TimeSpan PendingOrdersCutOff { get; set; }
        }
    }
}