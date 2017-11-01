using System.Threading.Tasks;
using MarginTrading.AccountReportsBroker.AzureRepositories.Entities;

namespace MarginTrading.AccountReportsBroker.AzureRepositories
{
    public interface IAccountsReportsRepository
    {
        Task InsertOrReplaceAsync(AccountsReportEntity report);
    }
}