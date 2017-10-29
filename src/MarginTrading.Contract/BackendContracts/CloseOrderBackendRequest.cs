namespace MarginTrading.Contract.BackendContracts
{
    public class CloseOrderBackendRequest
    {
        public string ClientId { get; set; }
        public string OrderId { get; set; }
        public string AccountId { get; set; }
    }
}
