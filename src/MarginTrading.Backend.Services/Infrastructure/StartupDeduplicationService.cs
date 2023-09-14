// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace MarginTrading.Backend.Services.Infrastructure
{
    /// <inheritdoc />
    /// <summary>
    /// Ensure that only single instance of the app is running.
    /// </summary>
    public class StartupDeduplicationService : IDisposable
    {
        private const string LockKey = "TradingEngine:DeduplicationLock";
        private readonly string _lockValue = Environment.MachineName;
        private readonly IWebHostEnvironment _hostingEnvironment; 
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly IDatabase _database;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public StartupDeduplicationService(
            IWebHostEnvironment hostingEnvironment,
            MarginTradingSettings marginTradingSettings,
            IConnectionMultiplexer redis)
        {
            _hostingEnvironment = hostingEnvironment;
            _marginTradingSettings = marginTradingSettings;
            _database = redis.GetDatabase();
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

            if (!_database.LockTake(LockKey, _lockValue, _marginTradingSettings.DeduplicationLockExpiryPeriod))
            {
                throw new Exception("Trading Engine failed to start due to deduplication validation failure.");
                // exception is logged by the global handler
            }

            Exception workerException = null;
            // ReSharper disable once PossibleNullReferenceException
            _cancellationTokenSource.Token.Register(() => throw workerException);
            
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        // wait and extend lock
                        await Task.Delay(_marginTradingSettings.DeduplicationLockExtensionPeriod);

                        await _database.LockExtendAsync(LockKey, _lockValue,
                            _marginTradingSettings.DeduplicationLockExpiryPeriod);
                    }
                }
                catch (Exception exception)
                {
                    workerException = exception;
                    _cancellationTokenSource.Cancel();
                }
            });
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }
    }
}