using System;
using System.Collections.Generic;
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
        private readonly IAlertService _alertService;

        public RepeatedProblemsOrderbooksService(IPriceCalcSettingsService priceCalcSettingsService, IAlertService alertService)
        {
            _priceCalcSettingsService = priceCalcSettingsService;
            _alertService = alertService;
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
                k => ImmutableSortedSet.Create(Event.ComparerByTime, newEvent),
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
                    {
                        _alertService.AlertRiskOfficer(
                            $"{orderbook.ExchangeName} is a repeated outlier exchange for {orderbook.AssetPairId}.\r\n" +
                            $"It had {outliersInRow} outlier orderbooks in a row during last {repeatedOutliersParams.MaxSequenceAge.TotalSeconds:f0} secs.");
                        Trace.Write("Repeated outlier (sequence)", new { orderbook.AssetPairId, orderbook.ExchangeName, outliersInRow, repeatedOutliersParams.MaxSequenceLength });
                        return true;
                    }

                    if (e.Time >= outlierAvgStart)
                    {
                        statsCount++;
                        if (e.IsOutlier)
                            outliersCount++;
                    }
                }

                var avg = outliersCount / (decimal) statsCount;
                if (avg > repeatedOutliersParams.MaxAvg)
                {
                    _alertService.AlertRiskOfficer(
                        $"{orderbook.ExchangeName} is a repeated outlier exchange for {orderbook.AssetPairId}.\r\n" +
                        $"It had {avg * 100:f4}% (i.e. {outliersCount} / {statsCount}) of max {repeatedOutliersParams.MaxAvg * 100:f4}% outlier orderbooks during last {repeatedOutliersParams.MaxAvgAge.TotalSeconds:f0} secs.");
                    Trace.Write("Repeated outlier (avg)", new { orderbook.AssetPairId, orderbook.ExchangeName, outliersCount, statsCount, avg, repeatedOutliersParams.MaxAvg });
                    return true;
                }
            }

            return false;
        }

        private static ImmutableSortedSet<Event> AddEventAndCleanOld(ImmutableSortedSet<Event> events, Event ev,
            DateTime minEventTime)
        {
            if (events[0].Time < minEventTime)
                return events.SkipWhile(e => e.Time < minEventTime).Concat(new[] { ev }).ToImmutableSortedSet(Event.ComparerByTime);
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

            private sealed class TimeComparer : IComparer<Event>
            {
                public int Compare(Event x, Event y)
                {
                    return Comparer<DateTime?>.Default.Compare(x?.Time, y?.Time);
                }
            }

            public static IComparer<Event> ComparerByTime { get; } = new TimeComparer();
        }
    }
}