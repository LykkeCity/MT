using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Common.Services;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class AssetDayOffTests
    {
        private const string AssetWithDayOff = "EURUSD";
        private const string AssetWithoutDayOff = "BTCUSD";

        public static IEnumerable DayOffTestCases
        {
            get
            {
                yield return new TestCaseData(new DateTime(2017, 6, 21), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 59, 59), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 21, 0, 0), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 24), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 20, 59, 59), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 21, 0, 0), AssetWithDayOff).Returns(false);

                yield return new TestCaseData(new DateTime(2017, 6, 21), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 59, 59), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 21, 0, 0), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 24), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 20, 59, 59), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 21, 0, 0), AssetWithoutDayOff).Returns(false);
            }
        }

        [Test]
        [TestCaseSource(nameof(DayOffTestCases))]
        public bool TestDayOff(DateTime dateTime, string asset)
        {
            //arrange 
            var dateService = new Mock<IDateService>();
            dateService.Setup(s => s.Now()).Returns(dateTime);
            var settings = new ScheduleSettings(
                dayOffStartDay: DayOfWeek.Friday,
                dayOffStartTime: new TimeSpan(21, 0, 0),
                dayOffEndDay: DayOfWeek.Sunday,
                dayOffEndTime: new TimeSpan(21, 0, 0),
                assetPairsWithoutDayOff: new[] {AssetWithoutDayOff, "BTCCHF"}.ToHashSet(),
                pendingOrdersCutOff: TimeSpan.Zero);
            var dayOffSettingsService = new Mock<IDayOffSettingsService>();
            dayOffSettingsService.Setup(s => s.GetScheduleSettings()).Returns(settings);
            dayOffSettingsService.Setup(s => s.GetExclusions(It.IsNotNull<string>())).Returns(ImmutableArray<DayOffExclusion>.Empty);
            var dayOffService = new AssetPairDayOffService(dateService.Object, dayOffSettingsService.Object);

            //act
            return dayOffService.IsDayOff(asset);
        }
        
        
        public static IEnumerable PendingOrdersDisabledTestCases
        {
            get
            {
                yield return new TestCaseData(new DateTime(2017, 6, 21), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 19, 59, 59), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 0, 0), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 24), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 21, 59, 59), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 22, 0, 0), AssetWithDayOff).Returns(false);

                yield return new TestCaseData(new DateTime(2017, 6, 21), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 19, 59, 59), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 0, 0), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 24), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 21, 59, 59), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 22, 0, 0), AssetWithoutDayOff).Returns(false);
            }
        }

        [Test]
        [TestCaseSource(nameof(PendingOrdersDisabledTestCases))]
        public bool TestPendingOrdersDisabled(DateTime dateTime, string asset)
        {
            //arrange 
            var dateService = new Mock<IDateService>();
            dateService.Setup(s => s.Now()).Returns(dateTime);
            var settings = new ScheduleSettings(
                dayOffStartDay: DayOfWeek.Friday,
                dayOffStartTime: new TimeSpan(21, 0, 0),
                dayOffEndDay: DayOfWeek.Sunday,
                dayOffEndTime: new TimeSpan(21, 0, 0),
                assetPairsWithoutDayOff: new[] {AssetWithoutDayOff, "BTCCHF"}.ToHashSet(),
                pendingOrdersCutOff: new TimeSpan(1, 0, 0));

            var dayOffSettingsService = new Mock<IDayOffSettingsService>();
            dayOffSettingsService.Setup(s => s.GetScheduleSettings()).Returns(settings);
            dayOffSettingsService.Setup(s => s.GetExclusions(It.IsNotNull<string>())).Returns(ImmutableArray<DayOffExclusion>.Empty);
            var dayOffService = new AssetPairDayOffService(dateService.Object, dayOffSettingsService.Object);

            //act
            return dayOffService.ArePendingOrdersDisabled(asset);
        }
        
        public static IEnumerable ExclusionsTestCases
        {
            get
            {
                yield return new TestCaseData(new DateTime(2017, 6, 21), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 19, 59, 59), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 00, 00), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 24, 01, 59, 59), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 24, 02, 00, 00), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 24, 03, 00, 00), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 24, 03, 00, 01), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 21, 59, 59), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 22, 00, 00), AssetWithDayOff).Returns(false);

                yield return new TestCaseData(new DateTime(2017, 6, 21), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 19, 59, 59), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 00, 00), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 24, 01, 59, 59), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 24, 02, 00, 00), AssetWithoutDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 24, 05, 00, 00), AssetWithoutDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 24, 05, 00, 01), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 21, 59, 59), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 22, 00, 00), AssetWithoutDayOff).Returns(false);
            }
        }
        
        [Test]
        [TestCaseSource(nameof(ExclusionsTestCases))]
        public bool TestExclusions(DateTime dateTime, string asset)
        {
            //arrange 
            var dateService = new Mock<IDateService>();
            dateService.Setup(s => s.Now()).Returns(dateTime);
            var settings = new ScheduleSettings(
                dayOffStartDay: DayOfWeek.Friday,
                dayOffStartTime: new TimeSpan(21, 0, 0),
                dayOffEndDay: DayOfWeek.Sunday,
                dayOffEndTime: new TimeSpan(21, 0, 0),
                assetPairsWithoutDayOff: new[] {AssetWithoutDayOff, "BTCCHF"}.ToHashSet(),
                pendingOrdersCutOff: new TimeSpan(1, 0, 0));

            var dayOffSettingsService = new Mock<IDayOffSettingsService>();
            dayOffSettingsService.Setup(s => s.GetScheduleSettings()).Returns(settings);

            dayOffSettingsService.Setup(s => s.GetExclusions(AssetWithDayOff))
                .Returns(ImmutableArray.Create(
                    new DayOffExclusion(Guid.NewGuid(), "smth",
                        new DateTime(2017, 6, 24, 01, 00, 00),
                        new DateTime(2017, 6, 24, 04, 00, 00), true)));
            
            dayOffSettingsService.Setup(s => s.GetExclusions(AssetWithoutDayOff))
                .Returns(ImmutableArray.Create(
                    new DayOffExclusion(Guid.NewGuid(), "smth",
                        new DateTime(2017, 6, 24, 03, 00, 00),
                        new DateTime(2017, 6, 24, 04, 00, 00), false)));

            var dayOffService = new AssetPairDayOffService(dateService.Object, dayOffSettingsService.Object);

            //act
            return dayOffService.ArePendingOrdersDisabled(asset);
        }
    }
}
