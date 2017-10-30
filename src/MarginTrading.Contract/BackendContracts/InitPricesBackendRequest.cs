namespace MarginTrading.Contract.BackendContracts
{
    public class InitPricesBackendRequest : ClientIdBackendRequest
    {
        public string[] AssetIds { get; set; }
    }
}
