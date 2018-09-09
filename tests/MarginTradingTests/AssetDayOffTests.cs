using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.DayOffSettings;
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
        
        private static readonly ScheduleSettings[] ScheduleSettings = {
            new ScheduleSettings
            {
                Id = "ConcreteOneDayOn5",
                Rank = 5,
                IsTradeEnabled = true,
                PendingOrdersCutOff = TimeSpan.FromMinutes(1),
                Start = new ScheduleConstraint
                {
                    Date = new DateTime(2017, 6, 23), //friday
                    DayOfWeek = null,
                    Time = TimeSpan.Zero
                },
                End = new ScheduleConstraint
                {
                    Date = new DateTime(2017, 6, 24),
                    DayOfWeek = null,
                    Time = TimeSpan.Zero
                }
            },
            new ScheduleSettings
            {
                Id = "RecurringWeekendOff2",
                Rank = 2,
                IsTradeEnabled = false,
                PendingOrdersCutOff = null,
                Start = new ScheduleConstraint
                {
                    Date = null,
                    DayOfWeek = DayOfWeek.Friday,
                    Time = TimeSpan.FromHours(21)
                },
                End = new ScheduleConstraint
                {
                    Date = null,
                    DayOfWeek = DayOfWeek.Sunday,
                    Time = TimeSpan.FromHours(21)
                }
            },
            new ScheduleSettings
            {
                Id = "ConcreteOneDayOn1",
                Rank = 1,
                IsTradeEnabled = true,
                PendingOrdersCutOff = TimeSpan.FromMinutes(1),
                Start = new ScheduleConstraint
                {
                    Date = new DateTime(2017, 6, 23), //friday
                    DayOfWeek = null,
                    Time = TimeSpan.Zero
                },
                End = new ScheduleConstraint
                {
                    Date = new DateTime(2017, 6, 24),
                    DayOfWeek = null,
                    Time = TimeSpan.Zero
                }
            },
        };

        private static IEnumerable DayOffTestCases
        {
            [UsedImplicitly]
            get
            {
                yield return new TestCaseData(new DateTime(2017, 6, 21), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 59, 59), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 21, 0, 1), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 24), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 20, 59, 59), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 21, 0, 1), AssetWithDayOff).Returns(false);

                yield return new TestCaseData(new DateTime(2017, 6, 21), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 59, 59), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 21, 0, 1), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 24), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 20, 59, 59), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 21, 0, 1), AssetWithoutDayOff).Returns(false);
            }
        }

//        [Test]
//        [TestCaseSource(nameof(DayOffTestCases))]
//        public bool TestDayOff(DateTime dateTime, string asset)
//        {
//            //arrange 
//            var dateService = new Mock<IDateService>();
//            dateService.Setup(s => s.Now()).Returns(dateTime);
//            
//            var scheduleSettingsCacheService = new Mock<IScheduleSettingsCacheService>();
//            scheduleSettingsCacheService.Setup(s => s.GetCompiledScheduleSettings(It.IsIn(AssetWithoutDayOff), 
//                    dateService.Object.Now(), TimeSpan.Zero))
//                .Returns(new List<ScheduleSettings>());
//            scheduleSettingsCacheService.Setup(s => s.GetCompiledScheduleSettings(It.IsIn(AssetWithDayOff), 
//                    dateService.Object.Now(), TimeSpan.Zero))
//                .Returns(new List<ScheduleSettings> {ScheduleSettings[1]});
//            var dayOffService = new AssetPairDayOffService(dateService.Object, scheduleSettingsCacheService.Object);
//
//            //act
//            return dayOffService.IsDayOff(asset);
//        }
//
//        private static IEnumerable IntersectedSpecialHigherTestCases
//        {
//            [UsedImplicitly]
//            get
//            {
//                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 59, 59), AssetWithDayOff).Returns(false);
//                yield return new TestCaseData(new DateTime(2017, 6, 23, 21, 0, 1), AssetWithDayOff).Returns(false);
//                yield return new TestCaseData(new DateTime(2017, 6, 23, 23, 59, 59), AssetWithDayOff).Returns(false);
//                yield return new TestCaseData(new DateTime(2017, 6, 24, 0, 0, 1), AssetWithDayOff).Returns(true);
//            }
//        }
//        
//        [Test]
//        [TestCaseSource(nameof(IntersectedSpecialHigherTestCases))]
//        public bool TestIntersectionSpecialHigher(DateTime dateTime, string asset)
//        {
//            //arrange 
//            var dateService = new Mock<IDateService>();
//            dateService.Setup(s => s.Now()).Returns(dateTime);
//            
//            var scheduleSettingsCacheService = new Mock<IScheduleSettingsCacheService>();
//            scheduleSettingsCacheService.Setup(s => s.GetCompiledScheduleSettings(It.IsIn(AssetWithoutDayOff), TODO, TODO))
//                .Returns(new List<ScheduleSettings>());
//            scheduleSettingsCacheService.Setup(s => s.GetCompiledScheduleSettings(It.IsIn(AssetWithDayOff), TODO, TODO))
//                .Returns(new List<ScheduleSettings> {ScheduleSettings[0], ScheduleSettings[1]});
//            var dayOffService = new AssetPairDayOffService(dateService.Object, scheduleSettingsCacheService.Object);
//            
//            //act
//            return dayOffService.IsDayOff(asset);
//        }
//
//        private static IEnumerable IntersectedSpecialLowerTestCases
//        {
//            [UsedImplicitly]
//            get
//            {
//                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 59, 59), AssetWithDayOff).Returns(false);
//                yield return new TestCaseData(new DateTime(2017, 6, 23, 21, 0, 1), AssetWithDayOff).Returns(true);
//                yield return new TestCaseData(new DateTime(2017, 6, 23, 23, 59, 59), AssetWithDayOff).Returns(true);
//                yield return new TestCaseData(new DateTime(2017, 6, 24, 0, 0, 1), AssetWithDayOff).Returns(true);
//            }
//        }
//        
//        [Test]
//        [TestCaseSource(nameof(IntersectedSpecialLowerTestCases))]
//        public bool TestIntersectionSpecialLower(DateTime dateTime, string asset)
//        {
//            //arrange 
//            var dateService = new Mock<IDateService>();
//            dateService.Setup(s => s.Now()).Returns(dateTime);
//            
//            var scheduleSettingsCacheService = new Mock<IScheduleSettingsCacheService>();
//            scheduleSettingsCacheService.Setup(s => s.GetCompiledScheduleSettings(It.IsIn(AssetWithoutDayOff), TODO, TODO))
//                .Returns(new List<ScheduleSettings>());
//            scheduleSettingsCacheService.Setup(s => s.GetCompiledScheduleSettings(It.IsIn(AssetWithDayOff), TODO, TODO))
//                .Returns(new List<ScheduleSettings> {ScheduleSettings[1], ScheduleSettings[2]});
//            var dayOffService = new AssetPairDayOffService(dateService.Object, scheduleSettingsCacheService.Object);
//            
//            //act
//            return dayOffService.IsDayOff(asset);
//        }
        
        
        
        
        
        
        //todo fix it
//        public static IEnumerable PendingOrdersDisabledTestCases
//        {
//            [UsedImplicitly]
//            get
//            {
//                yield return new TestCaseData(new DateTime(2017, 6, 21), AssetWithDayOff).Returns(false);
//                yield return new TestCaseData(new DateTime(2017, 6, 23, 19, 59, 59), AssetWithDayOff).Returns(false);
//                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 0, 0), AssetWithDayOff).Returns(true);
//                yield return new TestCaseData(new DateTime(2017, 6, 24), AssetWithDayOff).Returns(true);
//                yield return new TestCaseData(new DateTime(2017, 6, 25, 21, 59, 59), AssetWithDayOff).Returns(true);
//                yield return new TestCaseData(new DateTime(2017, 6, 25, 22, 0, 0), AssetWithDayOff).Returns(false);
//
//                yield return new TestCaseData(new DateTime(2017, 6, 21), AssetWithoutDayOff).Returns(false);
//                yield return new TestCaseData(new DateTime(2017, 6, 23, 19, 59, 59), AssetWithoutDayOff).Returns(false);
//                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 0, 0), AssetWithoutDayOff).Returns(false);
//                yield return new TestCaseData(new DateTime(2017, 6, 24), AssetWithoutDayOff).Returns(false);
//                yield return new TestCaseData(new DateTime(2017, 6, 25, 21, 59, 59), AssetWithoutDayOff).Returns(false);
//                yield return new TestCaseData(new DateTime(2017, 6, 25, 22, 0, 0), AssetWithoutDayOff).Returns(false);
//            }
//        }
//
//        [Test]
//        [TestCaseSource(nameof(PendingOrdersDisabledTestCases))]
//        public bool TestPendingOrdersDisabled(DateTime dateTime, string asset)
//        {
//            //arrange 
//            var dateService = new Mock<IDateService>();
//            dateService.Setup(s => s.Now()).Returns(dateTime);
//
//            var scheduleSettingsCacheService = new Mock<IScheduleSettingsCacheService>();
//            scheduleSettingsCacheService.Setup(s => s.GetScheduleSettings(It.IsIn(AssetWithoutDayOff)))
//                .Returns(new List<ScheduleSettings>());
//            scheduleSettingsCacheService.Setup(s => s.GetScheduleSettings(It.IsIn(AssetWithDayOff)))
//                .Returns(new List<ScheduleSettings> {ScheduleSettings[1]});
//            var dayOffService = new AssetPairDayOffService(dateService.Object, scheduleSettingsCacheService.Object);
//            
//            //act
//            return dayOffService.ArePendingOrdersDisabled(asset);
//        }
    }
}
