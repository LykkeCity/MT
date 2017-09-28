namespace MarginTrading.DataReader.Models
{
    public class SummaryAssetInfo
    {
        public string AssetPairId { get; set; }
        public double VolumeLong { get; set; }
        public double VolumeShort { get; set; }
        public double PnL { get; set; }
    }
}
