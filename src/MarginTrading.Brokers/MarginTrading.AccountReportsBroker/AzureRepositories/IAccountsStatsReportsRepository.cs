using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.AccountReportsBroker.AzureRepositories.Entities;

namespace MarginTrading.AccountReportsBroker.AzureRepositories
{
    public interface IAccountsStatsReportsRepository
    {
        Task InsertOrReplaceBatchAsync(IEnumerable<AccountsStatReportEntity> stats);
    }
}