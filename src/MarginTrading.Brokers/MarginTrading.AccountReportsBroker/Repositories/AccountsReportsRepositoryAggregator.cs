using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.AccountReportsBroker.Repositories.Models;

namespace MarginTrading.AccountReportsBroker.Repositories
{
    internal class AccountsReportsRepositoryAggregator: IAccountsReportsRepository
    {
        private readonly List<IAccountsReportsRepository> _repositories;

        public AccountsReportsRepositoryAggregator(IEnumerable<IAccountsReportsRepository> repositories)
        {
            _repositories = new List<IAccountsReportsRepository>();
            _repositories.AddRange(repositories);
        }

        public async Task InsertOrReplaceAsync(IAccountsReport report)
        {
            foreach (var item in _repositories)
            {
                await item.InsertOrReplaceAsync(report);
            }
        }
    }
}
