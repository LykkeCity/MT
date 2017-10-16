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

            TimeSpan maxEventsAge = _priceCalcSettingsService.GetMaxOutlierEventsAge();
            var newEvent = new Event(now, isOutdated, isOutlier);
            var actualProblems = _lastEvents.AddOrUpdate((orderbook.AssetPairId, orderbook.ExchangeName),
                k => ImmutableSortedSet.Create(newEvent),
                (k, old) => AddEventAndCleanOld(old, newEvent, maxEventsAge));

            //currently we process only Outlier
            if (isOutlier)
            {
                int maxOutlierSequenceLength = _priceCalcSettingsService.GetMaxOutlierSequenceLength();
                int maxOutlierSequenceAge = _priceCalcSettingsService.GetMaxOutlierSequenceAge();
                int outliersInRow = 0;
                decimal stats;
                foreach (var e in actualProblems)
                {
                    if (e.IsOutlier)
                        outliersInRow++;
                    else
                        outliersInRow = 0;

                    if (outliersInRow > maxOutlierSequenceLength)
                        return true;

                    if ()
                }
            }
        }

        private static ImmutableSortedSet<Event> AddEventAndCleanOld(ImmutableSortedSet<Event> events, Event ev,
            TimeSpan maxEventsAge)
        {
            var minTime = ev.Time - maxEventsAge;
            if (events[0].Time < minTime)
            {
                return events.SkipWhile(e => e.Time < minTime).Concat(new[] { ev }).ToImmutableSortedSet();
            }
            else
            {
                return events.Add(ev);
            }
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