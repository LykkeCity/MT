using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
    public interface IOperationLog
    {
        string Name { get; }
        string ClientId { get; }
        string AccountId { get; }
        string Input { get; }
        string Data { get; }
    }

    public class OperationLog : IOperationLog
    {
        public string Name { get; set; }
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public string Input { get; set; }
        public string Data { get; set; }
    }

    public interface IMarginTradingOperationsLogRepository
    {
        Task AddLogAsync(IOperationLog logEntity);
    }
}
