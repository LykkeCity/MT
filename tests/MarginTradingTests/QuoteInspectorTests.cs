// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using FluentAssertions;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Quotes;
using MarginTrading.Common.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class QuoteInspectorTests
    {
        private IDateService _dateService;
        private static readonly TimeSpan StalePeriod = TimeSpan.FromSeconds(5);
        
        
        [SetUp]
        public void SetUp()
        {
            _dateService = new DateService();
        }
        
        [Test]
        [TestCaseSource(nameof(IsQuoteStaleTestCases))]
        public void IsQuoteStale_WhenQuoteIsStale_ShouldReturnTrue(string quoteTimestamp,
            string now,
            string stalePeriod)
        {
            var quoteTimestampParsed = DateTime.Parse(quoteTimestamp);
            var nowParsed = DateTime.Parse(now);
            var stalePeriodParsed = TimeSpan.Parse(stalePeriod);

            QuoteCacheInspector.IsQuoteStale(quoteTimestampParsed, nowParsed, stalePeriodParsed).Should().BeTrue();
        }

        [Test]
        public void CanWarn_WhenQuoteIsNull_ShouldReturnFalse()
        {
            var sut = CreateSut();

            sut.CanWarn(null).Should().BeFalse();
        }

        [TestCase(1)]
        [TestCase(100)]
        [TestCase(100000)]
        public void CanWarn_WhenQuoteIsStale_ShouldReturnTrue(int addSeconds)
        {
            var sut = CreateSut();

            var staleQuoteDateTime = _dateService.Now() - StalePeriod.Add(TimeSpan.FromSeconds(addSeconds));

            var canWarn = sut.CanWarn(new InstrumentBidAskPair
                { Instrument = "whatever", Date = staleQuoteDateTime });
            
            canWarn.Should().BeTrue();
        }

        [Test]
        public void CanWarn_WhenSameQuote_ShouldReturnFalse()
        {
            var sut = CreateSut();

            var staleQuote = new InstrumentBidAskPair { Instrument = "whatever", Date = DateTime.MinValue };

            var canWarnFirstTime = sut.CanWarn(staleQuote);

            canWarnFirstTime.Should().BeTrue();
            sut.WarnOnStaleQuote(staleQuote);
            
            var canWarnSecondTime = sut.CanWarn(staleQuote);
            
            canWarnSecondTime.Should().BeFalse();
        }

        [Test]
        public void GetQuoteHash_Same_For_Instrument_And_Date()
        {
            var quoteDate = DateTime.MinValue;
            
            var quote1 = new InstrumentBidAskPair { Instrument = "whatever", Date = quoteDate };
            var quote2 = new InstrumentBidAskPair { Instrument = "whatever", Date = quoteDate, Ask = 1 };
            
            Assert.AreEqual(quote1.GetStaleHash(), quote2.GetStaleHash());
        }
        
        [Test]
        public void GetQuoteHash_Different_When_Date_Different()
        {
            var quoteDate = DateTime.MinValue;
            
            var quote1 = new InstrumentBidAskPair { Instrument = "whatever", Date = quoteDate };
            var quote2 = new InstrumentBidAskPair { Instrument = "whatever", Date = quoteDate.AddTicks(1) };
            
            Assert.AreNotEqual(quote1.GetStaleHash(), quote2.GetStaleHash());
        }

        public static object[] IsQuoteStaleTestCases =
        {
            new object[] { "2000-01-01", "2001-01-01", "00:00:05" },
            new object[] { "2000-01-01", "2000-01-01 00:00:06", "00:00:05" },
            new object[] { "2000-01-01 12:12:12", "2000-01-01 12:13:00", "00:00:05" },
            new object[] { "2000-01-01 12:00:00", "2000-01-11 12:00:00", "5.0:00:00" },
            new object[] { "2000-01-01", "2000-01-01 00:00:05.001", "00:00:05" },
        };

        private QuoteCacheInspector CreateSut()
        {
            return new QuoteCacheInspector(Mock.Of<IQuoteCacheService>(),
                _dateService,
                Mock.Of<ILogger<QuoteCacheInspector>>(),
                new MarginTradingSettings
                {
                    Monitoring = new MonitoringSettings
                    {
                        Quotes = new QuotesMonitoringSettings { ConsiderQuoteStalePeriod = StalePeriod }
                    }
                }
            );
        }
    }
}