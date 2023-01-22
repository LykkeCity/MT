using System;
using MarginTrading.Backend.Core.Extensions;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class DateTimeExtensionsTests
    {
        [Test]
        [TestCase("2023-01-18T10:00:00+0000", "2023-01-18T11:00:00+0000", "2023-01-18T11:00:00+0000")]
        [TestCase("2023-01-19T10:00:00+0000", "2023-01-18T11:00:00+0000", "2023-01-19T10:00:00+0000")]
        [TestCase("2023-01-18T10:29:00+0000", "2023-01-18T10:31:00+0000", "2023-01-18T10:31:00+0000")]
        [TestCase("2023-01-19T10:42:00+0000", "2023-01-19T10:39:00+0000", "2023-01-19T10:42:00+0000")]
        [TestCase("2023-01-20T10:00:00+0300", "2023-01-20T11:00:00+0500", "2023-01-20T10:00:00+0300")]
        [TestCase("2023-01-20T11:30:00+0300", "2023-01-20T13:29:00+0500", "2023-01-20T11:30:00+0300")]
        public void MaxDateTimeFunction_ShouldReturnTheMostRecentDateTime(string timestamp1, string timestamp2, string expected)
        {
            var dt1 = DateTime.Parse(timestamp1);
            var dt2 = DateTime.Parse(timestamp2);

            var result = DateTimeExtensions.MaxDateTime(dt1, dt2);

            Assert.AreEqual(DateTime.Parse(expected), result);
        }
    }
}