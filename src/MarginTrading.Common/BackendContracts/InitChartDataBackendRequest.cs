namespace MarginTrading.Common.BackendContracts
{
    public class InitChartDataBackendRequest : ClientIdBackendRequest
    {
        public string[] AssetIds { get; set; }
    }
}
