using System.Threading.Tasks;

namespace MarginTrading.Common.Services
{
    public interface IOperationLog
    {
        string Name { get; }
        string AccountId { get; }
        string Input { get; }
        string Data { get; }
    }

    public class OperationLog : IOperationLog
    {
        public string Name { get; set; }
        public string AccountId { get; set; }
        public string Input { get; set; }
        public string Data { get; set; }
    }

    public interface IOperationsLogRepository
    {
        Task AddLogAsync(IOperationLog logEntity);
    }
}
