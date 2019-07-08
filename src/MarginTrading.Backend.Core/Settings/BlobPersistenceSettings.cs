// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Core.Settings
{
    public class BlobPersistenceSettings
    {
        public int QuotesDumpPeriodMilliseconds { get; set; }
        public int FxRatesDumpPeriodMilliseconds { get; set; }
        public int OrderbooksDumpPeriodMilliseconds { get; set; }
        public int OrdersDumpPeriodMilliseconds { get; set; }
    }
}