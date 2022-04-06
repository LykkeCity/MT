// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core
{
    public class InstrumentTradingStatus
    {
        public bool TradingEnabled { get; }

        public InstrumentTradingDisabledReason Reason { get; }

        private InstrumentTradingStatus(bool tradingEnabled, InstrumentTradingDisabledReason reason)
        {
            TradingEnabled = tradingEnabled;
            Reason = reason;
        }

        public static InstrumentTradingStatus Enabled() =>
            new InstrumentTradingStatus(true, InstrumentTradingDisabledReason.None);

        public static InstrumentTradingStatus Disabled(InstrumentTradingDisabledReason reason) =>
            new InstrumentTradingStatus(false, reason);

        /// <summary>
        /// For backwards compatibility with isDayOff api
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public static implicit operator bool(InstrumentTradingStatus status) => !status.TradingEnabled;
    }
}