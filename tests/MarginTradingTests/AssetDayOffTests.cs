using System;
using System.Collections;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
using MarginTrading.Services;
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
            var settings = new MarketMakerSettings()
            {
                DayOffStartDay = DayOfWeek.Friday.ToString(),
                DayOffStartHour = 21,
                DayOffEndDay = DayOfWeek.Sunday.ToString(),
                DayOffEndHour = 21,
                AssetsWithoutDayOff = new [] {AssetWithoutDayOff,"BTCCHF"}
            };
            var dayOffService = new AssetDayOffService(dateService.Object, settings);

            //act
            return dayOffService.IsDayOff(asset);
        }
    }
}
