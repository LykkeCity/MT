using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Core.Settings
{
    public class AccountAssetsSettings
    {
        [Optional]
        public int LeverageInit { get; set; }

        [Optional]
        public int LeverageMaintenance { get; set; }

        [Optional]
        public decimal SwapLong { get; set; }

        [Optional]
        public decimal SwapShort { get; set; }

        [Optional]
        public decimal OvernightSwapLong { get; set; }

        [Optional]
        public decimal OvernightSwapShort { get; set; }

        [Optional]
        public decimal SwapLongPct { get; set; }

        [Optional]
        public decimal SwapShortPct { get; set; }

        [Optional]
        public decimal CommissionLong { get; set; }

        [Optional]
        public decimal CommissionShort { get; set; }

        [Optional]
        public decimal CommissionLot { get; set; }

        [Optional]
        public decimal DeltaBid { get; set; }

        [Optional]
        public decimal DeltaAsk { get; set; }

        [Optional]
        public decimal DealLimit { get; set; }

        [Optional]
        public decimal PositionLimit { get; set; }
    }
}