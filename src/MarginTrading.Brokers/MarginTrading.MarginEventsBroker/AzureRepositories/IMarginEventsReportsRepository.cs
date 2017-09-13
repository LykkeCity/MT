using System.Threading.Tasks;

namespace MarginTrading.MarginEventsBroker.AzureRepositories
{
    internal interface IMarginEventsReportsRepository
    {
        Task InsertOrReplaceAsync(MarginEventReport entity);
    }
}