namespace MarginTrading.Backend.Contracts.AccountBalance
{
    public class AccountDepositWithdrawRequest
    {
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public PaymentType PaymentType { get; set; }
        public decimal Amount { get; set; }
    }
}