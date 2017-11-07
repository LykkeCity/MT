using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MarginTrading.AccountMarginEventsBroker.Repositories.Models;

namespace MarginTrading.AccountMarginEventsBroker.Repositories
{
    internal class RepositoryAggregator : IAccountMarginEventsReportsRepository
    {
        private readonly List<IAccountMarginEventsReportsRepository> _repositories;

        public RepositoryAggregator(IEnumerable<IAccountMarginEventsReportsRepository> repositories)
        {
            _repositories = new List<IAccountMarginEventsReportsRepository>();
            _repositories.AddRange(repositories);
        }
        
        public async Task InsertOrReplaceAsync(IAccountMarginEventReport report)
        {
            foreach (var item in _repositories)
            {
                await item.InsertOrReplaceAsync(report);
            }
        }
    }
}
