using System;

namespace MarginTrading.AccountReportsBroker.Repositories.Models
{
    public interface IAccountsReport
    {
        string Id { get; }
        DateTime Date { get; }
        string TakerCounterpartyId { get; }
        string TakerAccountId { get; }
        string BaseAssetId { get; }
        bool IsLive { get; }
    }
}
