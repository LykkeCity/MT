// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Services
{
    internal static class QuoteDeviationInfoFormatter
    {
        public static string FormatDeviationStatistics(string assetPairId,
            QuoteTimeDeviationTracker.QuoteTimeDeviationAccumulator deviationAccumulatorAccumulator)
        {
            var totalDeviationTimeFormatted = FormattingUtils.FormatMilliseconds(deviationAccumulatorAccumulator.TotalDeviationMs);
            var averageDeviationTimeFormatted =
                FormattingUtils.FormatMilliseconds(deviationAccumulatorAccumulator.TotalDeviationMs / deviationAccumulatorAccumulator.QuotesCounter);
            
            var assetInfo = $"Asset: {assetPairId}".PadRight(100);
            var quotesInfo = $"Quotes: {deviationAccumulatorAccumulator.QuotesCounter}".PadRight(20);
            var totalDeviationTimeInfo = $"Total deviation time: {totalDeviationTimeFormatted}".PadRight(40);
            var averageDeviationTimeInfo = $"Average deviation time: {averageDeviationTimeFormatted}".PadRight(40);
            
            return $"{assetInfo} | {quotesInfo} | {totalDeviationTimeInfo} | {averageDeviationTimeInfo}";
        }
    }
}