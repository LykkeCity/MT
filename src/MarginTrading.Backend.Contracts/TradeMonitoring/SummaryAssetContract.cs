namespace MarginTrading.Backend.Contracts.TradeMonitoring
{
    public class SummaryAssetContract
    {
        public string AssetPairId { get; set; }
        public decimal VolumeLong { get; set; }
        public decimal VolumeShort { get; set; }
        public decimal PnL { get; set; }
    }
}
