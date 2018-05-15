namespace MarginTrading.Common.Services
{
    public interface IMarginTradingOperationsLogService
    {
        void AddLog(string name, string accountId, string input, string data);
    }
}
