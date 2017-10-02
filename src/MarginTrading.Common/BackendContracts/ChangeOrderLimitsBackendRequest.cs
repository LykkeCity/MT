namespace MarginTrading.Common.BackendContracts
{
    public class ChangeOrderLimitsBackendRequest
    {
        public string ClientId { get; set; }
        public string OrderId { get; set; }
        public string AccountId { get; set; }
        public decimal TakeProfit { get; set; }
        public decimal StopLoss { get; set; }
        public decimal ExpectedOpenPrice { get; set; }
    }
}
