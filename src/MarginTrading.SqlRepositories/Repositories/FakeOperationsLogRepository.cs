using System.Threading.Tasks;
using MarginTrading.Common.Services;

namespace MarginTrading.SqlRepositories.Repositories
{
    /// <summary>
    /// Does nothing
    /// </summary>
    public class FakeOperationsLogRepository : IOperationsLogRepository
    {
        public Task AddLogAsync(IOperationLog logEntity)
        {
            return Task.CompletedTask;
        }
    }
}