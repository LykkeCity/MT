using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MarginTrading.AccountReportsBroker.Repositories.Models;

namespace MarginTrading.AccountReportsBroker.Repositories
{
    internal class AccountsStatsReportsRepositoryAggregator: IAccountsStatsReportsRepository
    {
        private readonly List<IAccountsStatsReportsRepository> _repositories;

        public AccountsStatsReportsRepositoryAggregator(IEnumerable<IAccountsStatsReportsRepository> repositories)
        {
            _repositories = new List<IAccountsStatsReportsRepository>();
            _repositories.AddRange(repositories);
        }

        public async Task InsertOrReplaceBatchAsync(IEnumerable<IAccountsStatReport> stats)
        {
            foreach (var item in _repositories)
            {
                await item.InsertOrReplaceBatchAsync(stats);
            }
        }
    }
}
