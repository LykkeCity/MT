namespace MarginTrading.Contract.ClientContracts
{
    public class AccountHistoryRpcClientRequest : AccountHistoryFiltersClientRequest
    {
        public string Token { get; set; }
    }
}
