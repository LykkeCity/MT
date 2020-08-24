// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Services;
using MarginTrading.AssetService.Contracts;
using MarginTrading.AssetService.Contracts.Scheduling;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class AssetDayOffTests
    {
        private const string AssetWithDayOff = "EURUSD";
        private const string AssetWithoutDayOff = "BTCUSD";
        
        private static readonly CompiledScheduleSettingsContract[] ScheduleSettings = {
            new CompiledScheduleSettingsContract
            {
                Id = "ConcreteOneDayOn5",
                Rank = 5,
                IsTradeEnabled = true,
                PendingOrdersCutOff = TimeSpan.FromMinutes(1),
                Start = new ScheduleConstraintContract
                {
                    Date = new DateTime(2017, 6, 23), //friday
                    DayOfWeek = null,
                    Time = TimeSpan.Zero
                },
                End = new ScheduleConstraintContract
                {
                    Date = new DateTime(2017, 6, 24),
                    DayOfWeek = null,
                    Time = TimeSpan.Zero
                }
            },
            new CompiledScheduleSettingsContract
            {
                Id = "RecurringWeekendOff2",
                Rank = 2,
                IsTradeEnabled = false,
                PendingOrdersCutOff = null,
                Start = new ScheduleConstraintContract
                {
                    Date = null,
                    DayOfWeek = DayOfWeek.Friday,
                    Time = TimeSpan.FromHours(21)
                },
                End = new ScheduleConstraintContract
                {
                    Date = null,
                    DayOfWeek = DayOfWeek.Sunday,
                    Time = TimeSpan.FromHours(21)
                }
            },
            new CompiledScheduleSettingsContract
            {
                Id = "ConcreteOneDayOn1",
                Rank = 1,
                IsTradeEnabled = true,
                PendingOrdersCutOff = TimeSpan.FromMinutes(1),
                Start = new ScheduleConstraintContract
                {
                    Date = new DateTime(2017, 6, 23), //friday
                    DayOfWeek = null,
                    Time = TimeSpan.Zero
                },
                End = new ScheduleConstraintContract
                {
                    Date = new DateTime(2017, 6, 24),
                    DayOfWeek = null,
                    Time = TimeSpan.Zero
                }
            },
            new CompiledScheduleSettingsContract
            {
                Id = "RecurringDailyOff1",
                Rank = 1,
                IsTradeEnabled = false,
                PendingOrdersCutOff = TimeSpan.FromMinutes(1),
                Start = new ScheduleConstraintContract
                {
                    Date = null,
                    DayOfWeek = null,
                    Time = new TimeSpan(20, 30, 0),
                },
                End = new ScheduleConstraintContract
                {
                    Date = null,
                    DayOfWeek = null,
                    Time = new TimeSpan(08, 00, 0)
                }
            },
        };

        private static IEnumerable WeekendOffTestCases
        {
            [UsedImplicitly]
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
        [TestCaseSource(nameof(WeekendOffTestCases))]
        public bool TestWeekendOff(DateTime dateTime, string asset)
        {
            //arrange
            var dayOffService = ArrangeDayOffService(dateTime, new[] {ScheduleSettings[1]});

            //act
            return dayOffService.IsDayOff(asset);
        }

        private static IEnumerable DailyOffTestCases
        {
            [UsedImplicitly]
            get
            {//20:30 - 8:00
                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 29, 59), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 8, 0, 0), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 30, 0), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 24), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 7, 59, 59), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 8, 0, 0), AssetWithDayOff).Returns(false);

                yield return new TestCaseData(new DateTime(2017, 6, 21), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 59, 59), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 21, 0, 0), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 24), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 20, 59, 59), AssetWithoutDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 25, 21, 0, 0), AssetWithoutDayOff).Returns(false);
            }
        }

        [Test]
        [TestCaseSource(nameof(DailyOffTestCases))]
        public bool TestDailyOff(DateTime dateTime, string asset)
        {
            //arrange
            var dayOffService = ArrangeDayOffService(dateTime, new[] {ScheduleSettings[3]});

            //act
            return dayOffService.IsDayOff(asset);
        }

        private static IEnumerable IntersectedSpecialHigherTestCases
        {
            [UsedImplicitly]
            get
            {
                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 59, 59), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 21, 0, 0), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 23, 59, 59), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 24, 0, 0, 0), AssetWithDayOff).Returns(true);
            }
        }
        
        [Test]
        [TestCaseSource(nameof(IntersectedSpecialHigherTestCases))]
        public bool TestIntersectionSpecialHigher(DateTime dateTime, string asset)
        {
            //arrange
            var dayOffService = ArrangeDayOffService(dateTime, new[] {ScheduleSettings[0], ScheduleSettings[1]});
            
            //act
            return dayOffService.IsDayOff(asset);
        }

        private static IEnumerable IntersectedSpecialLowerTestCases
        {
            [UsedImplicitly]
            get
            {
                yield return new TestCaseData(new DateTime(2017, 6, 23, 20, 59, 59), AssetWithDayOff).Returns(false);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 21, 0, 0), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 23, 23, 59, 59), AssetWithDayOff).Returns(true);
                yield return new TestCaseData(new DateTime(2017, 6, 24, 0, 0, 0), AssetWithDayOff).Returns(true);
            }
        }
        
        [Test]
        [TestCaseSource(nameof(IntersectedSpecialLowerTestCases))]
        public bool TestIntersectionSpecialLower(DateTime dateTime, string asset)
        {
            //arrange
            var dayOffService = ArrangeDayOffService(dateTime, new[] {ScheduleSettings[1], ScheduleSettings[2]});
            
            //act
            return dayOffService.IsDayOff(asset);
        }

        private IAssetPairDayOffService ArrangeDayOffService(DateTime dateTime,
            IEnumerable<CompiledScheduleSettingsContract> withDayOffSchedules)
        {
            var dateService = new Mock<IDateService>();
            dateService.Setup(s => s.Now()).Returns(dateTime);

            var assetPairsCacheMock = new Mock<IAssetPairsCache>();
            assetPairsCacheMock.Setup(s => s.GetAllIds())
                .Returns(new[] {AssetWithDayOff, AssetWithoutDayOff}.ToImmutableHashSet());
            var scheduleSettingsApiMock = new Mock<IScheduleSettingsApi>();
            scheduleSettingsApiMock.Setup(s => s.StateList(It.IsAny<string[]>()))
                .ReturnsAsync(new List<CompiledScheduleContract>
                {
                    new CompiledScheduleContract
                    {
                        AssetPairId = AssetWithDayOff,
                        ScheduleSettings = withDayOffSchedules.ToList()
                    },
                    new CompiledScheduleContract
                    {
                        AssetPairId = AssetWithoutDayOff,
                        ScheduleSettings = new List<CompiledScheduleSettingsContract>()
                    }
                });
            
            var scheduleSettingsCacheService = new ScheduleSettingsCacheService(
                Mock.Of<ICqrsSender>(), scheduleSettingsApiMock.Object,
                assetPairsCacheMock.Object, dateService.Object, new EmptyLog(), new OvernightMarginSettings());
            
            scheduleSettingsCacheService.UpdateScheduleSettingsAsync().GetAwaiter().GetResult();
            return new AssetPairDayOffService(dateService.Object, scheduleSettingsCacheService);
        }
        
        
        
        
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
