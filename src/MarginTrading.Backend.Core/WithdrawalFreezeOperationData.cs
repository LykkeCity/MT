namespace MarginTrading.Backend.Core
{
    public class WithdrawalFreezeOperationData : OperationDataBase<OperationState>
    {
        public string AccountId { get; set; }
        public decimal Amount { get; set; }
    }
}