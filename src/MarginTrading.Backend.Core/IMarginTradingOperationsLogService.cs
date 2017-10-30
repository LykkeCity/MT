namespace MarginTrading.Backend.Core
{
    public interface IMarginTradingOperationsLogService
    {
        void AddLog(string name, string clientId, string accountId, string input, string data);
    }
}
