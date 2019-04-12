using System;
using System.Threading;
using Common.Log;
using Lykke.Common;
using MarginTrading.Backend.Core.Settings;
using Microsoft.AspNetCore.Hosting;
using StackExchange.Redis;

namespace MarginTrading.Backend.Services.Infrastructure
{
    /// <summary>
    /// Ensure that only single instance of the app is running.
    /// </summary>
    public class StartupDeduplicationService
    {
        private const string LockKey = "TradingEngine:DeduplicationLock";
        private readonly string _lockValue = Environment.MachineName;

        private readonly IHostingEnvironment _hostingEnvironment; 
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly MarginTradingSettings _marginTradingSettings;

        public StartupDeduplicationService(
            IHostingEnvironment hostingEnvironment,
            IThreadSwitcher threadSwitcher,
            MarginTradingSettings marginTradingSettings)
        {
            _hostingEnvironment = hostingEnvironment;
            _threadSwitcher = threadSwitcher;
            _marginTradingSettings = marginTradingSettings;
        }

        /// <summary>
        /// Check that no instance is currently running and hold Redis distributed lock during app lifetime.
        /// Does nothing in debug mode.
        /// Clarification.
        /// There is a possibility of race condition in case when Redis is used in clustered/replicated mode:
        /// - Client A acquires the lock in the master.
        /// - The master crashes before the write to the key is transmitted to the slave.
        /// - The slave gets promoted to master.
        /// - Client B acquires the lock to the same resource A already holds a lock for. SAFETY VIOLATION!
        /// But the probability of such situation is extremely small, so current implementation neglects it.
        /// In case if it is required to assure safety in clustered/replicated mode RedLock algorithm may be used.
        /// </summary>
        public void HoldLock()
        {
            if (_hostingEnvironment.IsDevelopment())
            {
                return;
            }

            _threadSwitcher.SwitchThread(() =>
            {
                IDatabase db = null;
                try
                {
                    var multiplexer = ConnectionMultiplexer.Connect(_marginTradingSettings.RedisSettings.Configuration);
                    db = multiplexer.GetDatabase();

                    if (!db.LockTake(LockKey, _lockValue, _marginTradingSettings.DeduplicationLockExpiryPeriod))
                    {
                        throw new Exception("Trading Engine failed to start due to deduplication validation failure.");
                    }
                    
                    while (true)
                    {
                        // wait and extend lock
                        Thread.Sleep(_marginTradingSettings.DeduplicationLockExtensionPeriod);

                        db.LockExtend(LockKey, _lockValue, _marginTradingSettings.DeduplicationLockExpiryPeriod);
                    }
                }
                // exceptions are logged by thread switcher
                finally
                {
                    db?.LockRelease(LockKey, _lockValue);
                }
            });
        }
    }
}