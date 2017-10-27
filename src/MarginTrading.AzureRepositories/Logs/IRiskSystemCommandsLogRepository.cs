using System.Threading.Tasks;

namespace MarginTrading.AzureRepositories.Logs
{
    public interface IRiskSystemCommandsLogRepository
    {
        Task AddProcessedAsync(string commandType, object rawCommand);
        Task AddErrorAsync(string commandType, object rawCommand, string errorMessage);
    }
}