// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using FluentAssertions;
using MarginTrading.Backend.Services.Quotes;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class QuoteInspectorTests
    {
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

        public static object[] IsQuoteStaleTestCases =
        {
            new object[] {"2000-01-01", "2001-01-01", "00:00:05"},
            new object[] {"2000-01-01", "2000-01-01 00:00:06", "00:00:05"},
            new object[] {"2000-01-01 12:12:12", "2000-01-01 12:13:00", "00:00:05"},
            new object[] {"2000-01-01 12:00:00", "2000-01-11 12:00:00", "5.0:00:00"},
            new object[] {"2000-01-01", "2000-01-01 00:00:05.001", "00:00:05"},
        };
    }
}