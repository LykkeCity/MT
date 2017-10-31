namespace MarginTrading.Contract.BackendContracts
{
    public class InitChartDataBackendRequest : ClientIdBackendRequest
    {
        public string[] AssetIds { get; set; }
    }
}
