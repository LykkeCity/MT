namespace MarginTrading.Backend.Contracts.AccountBalance
{
    public class AccountChargeManuallyRequest
    {
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
    }
}