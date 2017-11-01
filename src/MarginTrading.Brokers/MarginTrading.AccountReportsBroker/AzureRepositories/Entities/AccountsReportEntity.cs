using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AccountReportsBroker.AzureRepositories.Entities
{
    public class AccountsReportEntity : TableEntity
    {
        public string TakerCounterpartyId
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public string TakerAccountId
        {
            get => RowKey;
            set => RowKey = value;
        }

        public string BaseAssetId { get; set; }
        public bool IsLive { get; set; }
    }
}