using MarginTrading.AccountReportsBroker.Repositories.Models;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace MarginTrading.SqlMigration.Repositories.Azure.Models
{
    class AccountsReportEntity : TableEntity, IAccountsReport
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

        public string Id => TakerAccountId;
        public DateTime Date => Timestamp.DateTime;
        
    }
}
