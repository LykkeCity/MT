using System;

namespace MarginTrading.AccountReportsBroker.Repositories.Models
{
    public class AccountsReport : IAccountsReport
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public string TakerCounterpartyId { get; set; }
        public string TakerAccountId { get; set; }
        public string BaseAssetId { get; set; }
        public bool IsLive { get; set; }
        public string LegalEntity { get; set; }
    }
}
