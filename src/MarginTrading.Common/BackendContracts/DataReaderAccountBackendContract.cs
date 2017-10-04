namespace MarginTrading.Common.BackendContracts
{
    public class DataReaderAccountBackendContract
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public decimal Balance { get; set; }
        public decimal WithdrawTransferLimit { get; set; }
        public bool IsLive { get; set; }
    }
}
