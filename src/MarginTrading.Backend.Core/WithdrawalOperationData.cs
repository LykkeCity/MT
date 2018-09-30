namespace MarginTrading.Backend.Core
{
    public class WithdrawalOperationData : OperationData
    {
        public string AccountId { get; set; }
        public decimal Amount { get; set; }
    }
}