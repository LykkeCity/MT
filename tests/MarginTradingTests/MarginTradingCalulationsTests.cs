using MarginTrading.Services.Helpers;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class MarginTradingCalulationsTests
    {
        [Test]
        public void PointCalculationsCorrect()
        {
            decimal value = MarginTradingCalculations.GetVolumeFromPoints(1, 3);
            Assert.AreEqual(0.001, value);

            value = MarginTradingCalculations.GetVolumeFromPoints(10, 3);
            Assert.AreEqual(0.01, value);
        }
    }
}
