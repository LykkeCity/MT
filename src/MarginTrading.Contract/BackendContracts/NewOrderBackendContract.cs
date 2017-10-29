namespace MarginTrading.Contract.BackendContracts
{
    public class NewOrderBackendContract
    {
        public string AccountId { get; set; }
        public string Instrument { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
        public decimal Volume { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        public OrderFillTypeContract FillType { get; set; }
    }
}