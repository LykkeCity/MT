namespace MarginTrading.Contract.BackendContracts
{
    public class OpenOrderBackendRequest
    {
        public string ClientId { get; set; }
        public NewOrderBackendContract Order { get; set; }
    }
}
