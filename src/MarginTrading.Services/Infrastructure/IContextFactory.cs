namespace MarginTrading.Services.Infrastructure
{
    public interface IContextFactory
    {
        TradingSyncContext GetReadSyncContext(string source);
        TradingSyncContext GetWriteSyncContext(string source);
    }
}