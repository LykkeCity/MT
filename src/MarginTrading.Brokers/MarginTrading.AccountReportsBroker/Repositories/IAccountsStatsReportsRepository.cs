using MarginTrading.AccountReportsBroker.Repositories.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.AccountReportsBroker.Repositories
{
    public interface IAccountsStatsReportsRepository
    {
        Task InsertOrReplaceBatchAsync(IEnumerable<IAccountsStatReport> stats);
    }
}   