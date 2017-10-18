using System;
using System.Collections.Immutable;
using System.Linq;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class RepeatedProblemsOrderbooksService : IRepeatedProblemsOrderbooksService
    {
        private readonly ReadWriteLockedDictionary<(string AssetPairId, string Exchange), ImmutableSortedSet<Event>>
            _lastEvents = new ReadWriteLockedDictionary<(string, string), ImmutableSortedSet<Event>>();

        private readonly IPriceCalcSettingsService _priceCalcSettingsService;

        public RepeatedProblemsOrderbooksService(IPriceCalcSettingsService priceCalcSettingsService)
        {
            _priceCalcSettingsService = priceCalcSettingsService;
        }

        public bool IsRepeatedProblemsOrderbook(ExternalOrderbook orderbook, bool isOutdated, bool isOutlier,
            DateTime now)
        {
            var repeatedOutliersParams = _priceCalcSettingsService.GetRepeatedOutliersParams(orderbook.AssetPairId);
            DateTime outlierSequenceStart = now - repeatedOutliersParams.MaxSequenceAge;
            DateTime outlierAvgStart = now - repeatedOutliersParams.MaxAvgAge;
            DateTime minEventTime = outlierSequenceStart < outlierAvgStart ? outlierSequenceStart : outlierAvgStart;
            var newEvent = new Event(now, isOutdated, isOutlier);
            var actualProblems = _lastEvents.AddOrUpdate((orderbook.AssetPairId, orderbook.ExchangeName),
                k => ImmutableSortedSet.Create(newEvent),
                (k, old) => AddEventAndCleanOld(old, newEvent, minEventTime));

            //currently we process only Outlier
            if (isOutlier)
            {
                int outliersInRow = 0;
                int statsCount = 0;
                int outliersCount = 0;
                foreach (var e in actualProblems)
                {
                    if (e.IsOutlier && e.Time >= outlierSequenceStart)
                        outliersInRow++;
                    else
                        outliersInRow = 0;

                    if (outliersInRow > repeatedOutliersParams.MaxSequenceLength)
                        return true;

                    if (e.Time >= outlierSequenceStart)
                    {
                        statsCount++;
                        if (e.IsOutlier)
                            outliersCount++;
                    }
                }

                if (outliersCount / (decimal)statsCount > repeatedOutliersParams.MaxAvg)
                    return true;
            }

            return false;
        }

        private static ImmutableSortedSet<Event> AddEventAndCleanOld(ImmutableSortedSet<Event> events, Event ev,
            DateTime minEventTime)
        {
            if (events[0].Time < minEventTime)
                return events.SkipWhile(e => e.Time < minEventTime).Concat(new[] { ev }).ToImmutableSortedSet();
            else
                return events.Add(ev);
        }

        private class Event
        {
            public DateTime Time { get; }
            public bool IsOutdated { get; }
            public bool IsOutlier { get; }

            public Event(DateTime time, bool isOutdated, bool isOutlier)
            {
                Time = time;
                IsOutdated = isOutdated;
                IsOutlier = isOutlier;
            }
        }
    }
}