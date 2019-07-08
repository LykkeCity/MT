// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Services.Helpers;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class MarginTradingCalulationsTests
    {
        [Test]
        public void PointCalculationsCorrect()
        {
            var value = MarginTradingCalculations.GetVolumeFromPoints(1, 3);
            Assert.AreEqual(0.001, value);

            value = MarginTradingCalculations.GetVolumeFromPoints(10, 3);
            Assert.AreEqual(0.01, value);
        }
    }
}
