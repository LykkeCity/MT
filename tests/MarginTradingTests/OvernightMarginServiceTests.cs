using System;
using System.Collections;
using System.Collections.Generic;
using Autofac;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Core.Services;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class OvernightMarginServiceTests : BaseTests
    {
        private IOvernightMarginService _overnightMarginService;
        
        [OneTimeSetUp]
        public void SetUp()
        {
            RegisterDependencies();
            _overnightMarginService = Container.Resolve<IOvernightMarginService>();
        }

        [TestCaseSource(nameof(OvernightMarginTestData))]
        [Test]
        public (DateTime Warn, DateTime Start, DateTime End) TryGetOperatingInterval_Success(
            List<CompiledScheduleTimeInterval> platformTrading, DateTime currentDateTime, bool expectedResult)
        {
            var actualResult = _overnightMarginService.TryGetOperatingInterval(platformTrading, currentDateTime, 
                out var resultingInterval);
            
            Assert.AreEqual(expectedResult, actualResult);

            return resultingInterval;
        }

        private static IEnumerable OvernightMarginTestData
        {
            [UsedImplicitly]
            get
            {
                // degenerate case:
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>(), 
                        new DateTime(2019, 1, 12), false)
                    .Returns((default(DateTime), default(DateTime), default(DateTime)));
                
                // before Warn cases:
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0))
                    }, new DateTime(2019, 1, 12, 6, 59, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 7, 0, 0),
                        new DateTime(2019, 1, 12, 7, 30, 0),
                        default(DateTime)));
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0)),
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = true, Rank = 2 }, 
                            new DateTime(2019, 1, 12, 12, 0, 0), 
                            new DateTime(2019, 1, 12, 21, 0, 0))
                    }, new DateTime(2019, 1, 12, 6, 59, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 7, 0, 0),
                        new DateTime(2019, 1, 12, 7, 30, 0),
                        default(DateTime)));
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0)),
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = true, Rank = -100 }, 
                            new DateTime(2019, 1, 12, 12, 0, 0), 
                            new DateTime(2019, 1, 12, 21, 0, 0))
                    }, new DateTime(2019, 1, 12, 6, 59, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 7, 0, 0),
                        new DateTime(2019, 1, 12, 7, 30, 0),
                        default(DateTime)));
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0)),
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 2 }, 
                            new DateTime(2019, 1, 12, 12, 0, 0), 
                            new DateTime(2019, 1, 12, 23, 0, 0))
                    }, new DateTime(2019, 1, 12, 6, 59, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 7, 0, 0),
                        new DateTime(2019, 1, 12, 7, 30, 0),
                        default(DateTime)));
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0)),
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = true, Rank = 2 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 9, 0, 0))
                    }, new DateTime(2019, 1, 12, 6, 59, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 8, 0, 0),
                        new DateTime(2019, 1, 12, 8, 30, 0),
                        default(DateTime)));
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0)),
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = true, Rank = 2 }, 
                            new DateTime(2019, 1, 12, 8, 1, 0), 
                            new DateTime(2019, 1, 12, 9, 0, 0))
                    }, new DateTime(2019, 1, 12, 6, 59, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 7, 0, 0),
                        new DateTime(2019, 1, 12, 7, 30, 0),
                        default(DateTime)));
                
                // between Warn and Start cases:
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0))
                    }, new DateTime(2019, 1, 12, 7, 29, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 7, 0, 0),
                        new DateTime(2019, 1, 12, 7, 30, 0),
                        default(DateTime)));
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0)),
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = true, Rank = 2 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 9, 0, 0))
                    }, new DateTime(2019, 1, 12, 7, 29, 0), true)
                    .Returns((new DateTime(2019, 1, 12, 8, 0, 0),
                        new DateTime(2019, 1, 12, 8, 30, 0),
                        default(DateTime)));
                
                // between Start and End cases:
                yield return new TestCaseData(new List<CompiledScheduleTimeInterval>
                    {
                        new CompiledScheduleTimeInterval(new ScheduleSettings { IsTradeEnabled = false, Rank = 1 }, 
                            new DateTime(2019, 1, 12, 8, 0, 0), 
                            new DateTime(2019, 1, 12, 22, 0, 0))
                    }, new DateTime(2019, 1, 12, 8, 30, 0), true)
                    .Returns((default(DateTime),
                        default(DateTime),
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
                        default(DateTime),
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
                        default(DateTime),
                        new DateTime(2019, 1, 12, 10, 0, 0)));
            }
        }
    }
}