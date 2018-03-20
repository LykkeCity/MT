using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.TradeMonitoring
{
    [PublicAPI]
    public class AssetSummaryContract
    {
        public string AssetPairId { get; set; }
        public decimal VolumeLong { get; set; }
        public decimal VolumeShort { get; set; }
        public decimal PnL { get; set; }
    }
}
