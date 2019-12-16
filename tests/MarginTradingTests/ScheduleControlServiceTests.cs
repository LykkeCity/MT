// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Common.Services;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class ScheduleControlServiceTests
    {
        [TestCaseSource(nameof(TryGetClosestPointTestData))]
        [Test]
        public DateTime TryGetClosestPoint_Success(
            Dictionary<string, List<CompiledScheduleTimeInterval>> marketsSchedule, 
            DateTime currentDateTime)
        {
            var dateServiceMock = new Mock<IDateService>();
            dateServiceMock.Setup(x => x.Now()).Returns(currentDateTime);

            var sut = new ScheduleControlService(Mock.Of<IScheduleSettingsCacheService>(),
                Mock.Of<ILog>(), dateServiceMock.Object);

            return sut.TryGetClosestPoint(marketsSchedule, currentDateTime);
        }
        
        [TestCaseSource(nameof(GetOperatingIntervalTestData))]
        [Test]
        public (DateTime Start, DateTime End) TryGetOperatingInterval_Success(
            List<CompiledScheduleTimeInterval> platformTrading, DateTime currentDateTime, bool expectedResult)
        {
            var dateServiceMock = new Mock<IDateService>();
            dateServiceMock.Setup(x => x.Now()).Returns(currentDateTime);
            
            var sut = new ScheduleControlService(Mock.Of<IScheduleSettingsCacheService>(),
                Mock.Of<ILog>(), dateServiceMock.Object);
            var actualResult = sut.TryGetOperatingInterval(platformTrading, currentDateTime, 
                out var resultingInterval);
            
            Assert.AreEqual(expectedResult, actualResult);

            return resultingInterval;
        }

        private static IEnumerable TryGetClosestPointTestData
        {
            [UsedImplicitly]
            get
            {
                yield return new TestCaseData(new Dictionary<string, List<CompiledScheduleTimeInterval>>
                        {
                            {
                                "market1", new List<CompiledScheduleTimeInterval>
                                {
                                    new CompiledScheduleTimeInterval(new ScheduleSettings {IsTradeEnabled = false},
                                        new DateTime(2019, 12, 10, 9, 0, 0),
                                        new DateTime(2019, 12, 10, 11, 0, 0))
                                }
                            }
                        },
                        new DateTime(2019, 12, 10, 10, 0, 0))
                    .Returns(new DateTime(2019, 12, 10, 11, 0, 0));
                yield return new TestCaseData(new Dictionary<string, List<CompiledScheduleTimeInterval>>
                        {
                            {
                                "market1", new List<CompiledScheduleTimeInterval>
                                {
                                    new CompiledScheduleTimeInterval(new ScheduleSettings {IsTradeEnabled = false},
                                        new DateTime(2019, 12, 10, 10, 0, 0),
                                        new DateTime(2019, 12, 10, 11, 0, 0))
                                }
                            }
                        },
                        new DateTime(2019, 12, 10, 10, 0, 0, 500))
                    .Returns(new DateTime(2019, 12, 10, 11, 0, 0));
                yield return new TestCaseData(new Dictionary<string, List<CompiledScheduleTimeInterval>>
                        {
                            {
                                "market1", new List<CompiledScheduleTimeInterval>
                                {
                                    new CompiledScheduleTimeInterval(new ScheduleSettings {IsTradeEnabled = false},
                                        new DateTime(2019, 12, 10, 10, 0, 0),
                                        new DateTime(2019, 12, 10, 11, 0, 0))
                                }
                            }
                        },
                        new DateTime(2019, 12, 10, 11, 0, 0, 100))
                    .Returns(new DateTime(2019, 12, 11, 11, 0, 0, 100));
                yield return new TestCaseData(new Dictionary<string, List<CompiledScheduleTimeInterval>>
                        {
                            {
                                "market1", new List<CompiledScheduleTimeInterval>
                                {
                                    new CompiledScheduleTimeInterval(new ScheduleSettings {IsTradeEnabled = false},
                                        new DateTime(2019, 12, 10, 10, 0, 0),
                                        new DateTime(2019, 12, 10, 11, 0, 0))
                                }
                            }
                        },
                        new DateTime(2019, 12, 10, 11, 0, 0))
                    .Returns(new DateTime(2019, 12, 11, 11, 0, 0));
                yield return new TestCaseData(new Dictionary<string, List<CompiledScheduleTimeInterval>>
                        {
                            {
                                "market1", new List<CompiledScheduleTimeInterval>
                                {
                                    new CompiledScheduleTimeInterval(new ScheduleSettings {IsTradeEnabled = false},
                                        new DateTime(2019, 12, 10, 10, 0, 0),
                                        new DateTime(2019, 12, 10, 11, 0, 0)),
                                    new CompiledScheduleTimeInterval(new ScheduleSettings {IsTradeEnabled = false},
                                        new DateTime(2019, 12, 11, 10, 0, 0),
                                        new DateTime(2019, 12, 11, 11, 0, 0))
                                }
                            }
                        },
                        new DateTime(2019, 12, 10, 11, 0, 0))
                    .Returns(new DateTime(2019, 12, 11, 10, 0, 0));
                yield return new TestCaseData(new Dictionary<string, List<CompiledScheduleTimeInterval>>
                        {
                            {
                                "market1", new List<CompiledScheduleTimeInterval>
                                {
                                    new CompiledScheduleTimeInterval(new ScheduleSettings {IsTradeEnabled = false},
                                        new DateTime(2019, 12, 10, 10, 0, 0),
                                        new DateTime(2019, 12, 10, 11, 0, 1))
                                }
                            }
                        },
                        new DateTime(2019, 12, 10, 11, 0, 0, 100))
                    .Returns(new DateTime(2019, 12, 10, 11, 0, 1));
            }
        }

        private static IEnumerable GetOperatingIntervalTestData
        {
            [UsedImplicitly]
            get
            {
                // degenerate case:
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>(), 
                        new DateTime(2019, 1, 12), false)
                    .Returns((default(DateTime), default(DateTime)));
                
                // before Warn cases:
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0))
                    }, new DateTime(2019, 1, 12, 6, 59, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 8, 0, 0),
                        new DateTime(2019, 1, 12, 22, 0, 0)));
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0)),
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = true, Rank = 2 }, 
                            new DateTime(2019, 1, 12, 12, 0, 0), 
                            new DateTime(2019, 1, 12, 21, 0, 0))
                    }, new DateTime(2019, 1, 12, 6, 59, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 8, 0, 0),
                        new DateTime(2019, 1, 12, 22, 0, 0)));
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0)),
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = true, Rank = -100 }, 
                            new DateTime(2019, 1, 12, 12, 0, 0), 
                            new DateTime(2019, 1, 12, 21, 0, 0))
                    }, new DateTime(2019, 1, 12, 6, 59, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 8, 0, 0),
                        new DateTime(2019, 1, 12, 22, 0, 0)));
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0)),
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 2 }, 
                            new DateTime(2019, 1, 12, 12, 0, 0), 
                            new DateTime(2019, 1, 12, 23, 0, 0))
                    }, new DateTime(2019, 1, 12, 6, 59, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 8, 0, 0),
                        new DateTime(2019, 1, 12, 22, 0, 0)));
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0)),
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = true, Rank = 2 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 9, 0, 0))
                    }, new DateTime(2019, 1, 12, 6, 59, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 9, 0, 0),
                        new DateTime(2019, 1, 12, 22, 0, 0)));
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0)),
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = true, Rank = 2 }, 
                            new DateTime(2019, 1, 12, 8, 1, 0), 
                            new DateTime(2019, 1, 12, 9, 0, 0))
                    }, new DateTime(2019, 1, 12, 6, 59, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 8, 0, 0),
                        new DateTime(2019, 1, 12, 22, 0, 0)));
                
                // between Warn and Start cases:
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0))
                    }, new DateTime(2019, 1, 12, 7, 29, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 8, 0, 0),
                        new DateTime(2019, 1, 12, 22, 0, 0)));
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0)),
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = true, Rank = 2 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 9, 0, 0))
                    }, new DateTime(2019, 1, 12, 7, 29, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 9, 0, 0),
                        new DateTime(2019, 1, 12, 22, 0, 0)));
                
                // between Start and End cases:
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0))
                    }, new DateTime(2019, 1, 12, 8, 30, 0), true)
                    .Returns((default(DateTime),
                        new DateTime(2019, 1, 12, 22, 0, 0)));
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0)),
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = true, Rank = -1 }, 
                            new DateTime(2019, 1, 12, 10, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0))
                    }, new DateTime(2019, 1, 12, 8, 30, 0), true)
                    .Returns((default(DateTime),
                        new DateTime(2019, 1, 12, 22, 0, 0)));
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0)),
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = true, Rank = 2 }, 
                            new DateTime(2019, 1, 12, 10, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0))
                    }, new DateTime(2019, 1, 12, 8, 30, 0), true)
                    .Returns((default(DateTime),
                        new DateTime(2019, 1, 12, 10, 0, 0)));
            }
        }
    }
}