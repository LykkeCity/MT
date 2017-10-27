namespace MarginTrading.MarketMaker.Models.Api
{
    public class DisabledSettingsModel
    {
        public bool IsTemporarilyDisabled { get; set; }
        public string Reason { get; set; }
    }
}