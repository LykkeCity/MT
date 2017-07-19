namespace MarginTrading.Common.BackendContracts
{
    public class InitPricesBackendRequest : ClientIdBackendRequest
    {
        public string[] AssetIds { get; set; }
    }
}
