using MarginTrading.AccountReportsBroker.Repositories.Models;
using System.Threading.Tasks;

namespace MarginTrading.AccountReportsBroker.Repositories
{
    public interface IAccountsReportsRepository
    {
        Task InsertOrReplaceAsync(IAccountsReport report);
    }
}