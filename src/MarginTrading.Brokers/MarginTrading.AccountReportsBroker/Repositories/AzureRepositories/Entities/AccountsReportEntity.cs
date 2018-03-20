using MarginTrading.AccountReportsBroker.Repositories.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AccountReportsBroker.Repositories.AzureRepositories.Entities
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
        public string LegalEntity { get; set; }

        public static AccountsReportEntity Create(IAccountsReport src)
        {
            return new AccountsReportEntity
            {
                TakerCounterpartyId = src.TakerCounterpartyId,
                TakerAccountId = src.TakerAccountId,
                BaseAssetId = src.BaseAssetId,
                IsLive = src.IsLive,
                LegalEntity = src.LegalEntity,
            };
        }
    }
}