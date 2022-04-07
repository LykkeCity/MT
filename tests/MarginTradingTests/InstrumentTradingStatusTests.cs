// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class InstrumentTradingStatusTests
    {
        [Test]
        public void TradingStatus_Enabled()
        {
            var status = InstrumentTradingStatus.Enabled();

            Assert.True(status.TradingEnabled);
            Assert.That(status.Reason == InstrumentTradingDisabledReason.None);
        }

        [Test]
        public void TradingStatus_Disabled()
        {
            var reason = InstrumentTradingDisabledReason.InstrumentTradingDisabled;
            var status = InstrumentTradingStatus.Disabled(reason);

            Assert.False(status.TradingEnabled);
            Assert.That(status.Reason == reason);
        }

        [Test]
        public void TradingStatus_Enabled_BackwardsCompatibility()
        {
            var status = InstrumentTradingStatus.Enabled();

            Assert.False(status);
        }
        
        [Test]
        public void TradingStatus_Disabled_BackwardsCompatibility()
        {
            var reason = InstrumentTradingDisabledReason.InstrumentTradingDisabled;
            var status = InstrumentTradingStatus.Disabled(reason);

            Assert.True(status);
        }
    }
}