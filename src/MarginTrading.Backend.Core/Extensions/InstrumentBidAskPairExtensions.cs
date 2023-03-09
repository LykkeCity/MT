namespace MarginTrading.Backend.Core.Extensions
{
    public static class InstrumentBidAskPairExtensions
    {
        /// <summary>
        /// Calculates a hash for the bid/ask pair, which is used only to determine
        /// if the quote has already been warned about being stale. Probably, the
        /// implementation entirely correct only for the stale usage scenario.
        /// </summary>
        /// <param name="bidAskPair"></param>
        /// <returns></returns>
        public static int GetStaleHash(this InstrumentBidAskPair bidAskPair)
        {
            unchecked
            {
                int hash = 23;
                hash = hash * 31 + (bidAskPair?.Instrument.GetHashCode() ?? 0);
                hash = hash * 31 + (bidAskPair?.Date.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}