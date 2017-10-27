namespace MarginTrading.MarketMaker.Models.Api
{
    public class HedgingPreferenceModel
    {
        public string AssetPairId { get; set; }
        public string Exchange { get; set; }
        public decimal Preference { get; set; }
        public bool HedgingTemporarilyDisabled { get; set; }
    }
}
