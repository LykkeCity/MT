namespace MarginTrading.Backend.Core
{
    public class FplData
    {
        public decimal Fpl { get; set; }
        public decimal QuoteRate { get; set; }
        public decimal MarginRate { get; set; }
        public decimal MarginInit { get; set; }
        public decimal MarginMaintenance { get; set; }
        public decimal OpenCrossPrice { get; set; }
        public decimal CloseCrossPrice { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal TotalFplSnapshot { get; set; }
        public decimal SwapsSnapshot { get; set; }
        public int AccountBaseAssetAccuracy { get; set; }
        
        public int CalculatedHash { get; set; }
        public int ActualHash { get; set; }
    }
}
