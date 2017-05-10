namespace MarginTrading.Common.BackendContracts
{
    public class ChangeOrderLimitsBackendRequest
    {
        public string ClientId { get; set; }
        public string OrderId { get; set; }
        public string AccountId { get; set; }
        public double TakeProfit { get; set; }
        public double StopLoss { get; set; }
        public double ExpectedOpenPrice { get; set; }
    }
}
