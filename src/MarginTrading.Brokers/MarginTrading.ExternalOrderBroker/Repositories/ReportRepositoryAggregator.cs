using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.ExternalOrderBroker.Models;

namespace MarginTrading.ExternalOrderBroker.Repositories
{
    internal class ReportRepositoryAggregator : IExternalOrderReportRepository
    {
        private readonly List<IExternalOrderReportRepository> _repositories;

        public ReportRepositoryAggregator(IEnumerable<IExternalOrderReportRepository> repositories)
        {
            _repositories = new List<IExternalOrderReportRepository>();
            _repositories.AddRange(repositories);
        }

        public async Task InsertOrReplaceAsync(IExternalOrderReport report)
        {
            await Task.WhenAll(_repositories.Select(repo => repo.InsertOrReplaceAsync(report)));
        }
    }
}
