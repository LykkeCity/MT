namespace MarginTrading.Contract.BackendContracts.AccountsManagement
{
    public class SetTradingConditionModel
    {
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public string TradingConditionId { get; set; }
    }
}