using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.AccountHistoryBroker.Repositories.Models;

namespace MarginTrading.AccountHistoryBroker.Repositories
{
    internal class RepositoryAggregator : IAccountTransactionsReportsRepository
    {
        private readonly List<IAccountTransactionsReportsRepository> _repositories;

        public RepositoryAggregator(IEnumerable<IAccountTransactionsReportsRepository> repositories)
        {
            _repositories = new List<IAccountTransactionsReportsRepository>();
            _repositories.AddRange(repositories);
        }

        public async Task InsertOrReplaceAsync(IAccountTransactionsReport report)
        {
            foreach (var item in _repositories)
            {
                await item.InsertOrReplaceAsync(report);
            }
        }
    }
}
