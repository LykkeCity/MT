namespace MarginTrading.Services.Infrastructure
{
    public interface IContextFactory
    {
	    /// <summary>
	    /// Creates context for synchronization of trade operations (reading).
	    /// Usage of async/await inside context is prohibited:
	    /// under the hood it uses the Monitor.Enter() & Exit(), which are broken by awaits.
	    /// </summary>    
	    ///     
	    /// <remarks>
	    /// If async continuation happens to execute on a different thread - 
	    /// monitor will not be able to perform Exit(), and nested Enter() will lead to a deadlock.
	    /// This is because monitor tracks the thread which entered the lock.
	    /// Such code can work on dev environment, but can cause "magic" issues on production.
	    /// </remarks>
        TradingSyncContext GetReadSyncContext(string source);

	    /// <summary>
	    /// Creates context for synchronization of trade operations (writing).
	    /// Usage of async/await inside context is prohibited:
	    /// under the hood it uses the Monitor.Enter() & Exit(), which are broken by awaits.
	    /// </summary>    
	    ///     
	    /// <remarks>
	    /// If async continuation happens to execute on a different thread - 
	    /// monitor will not be able to perform Exit(), and nested Enter() will lead to a deadlock.
	    /// This is because monitor tracks the thread which entered the lock.
	    /// Such code can work on dev environment, but can cause "magic" issues on production.
	    /// </remarks>
        TradingSyncContext GetWriteSyncContext(string source);
    }
}