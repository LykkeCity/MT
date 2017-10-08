using System.Threading.Tasks;

namespace MarginTrading.AccountMarginEventsBroker.AzureRepositories
{
    internal interface IAccountMarginEventsReportsRepository
    {
        Task InsertOrReplaceAsync(AccountMarginEventReportEntity entity);
    }
}