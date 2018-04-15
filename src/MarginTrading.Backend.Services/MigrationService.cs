using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Migrations;

namespace MarginTrading.Backend.Services
{
    public class MigrationService : IMigrationService
    {
        private readonly IEnumerable<IMigration> _migrations;
        
        private readonly IMarginTradingBlobRepository _marginTradingBlobRepository;

        private readonly ILog _log;
        
        public MigrationService(
            IEnumerable<IMigration> migrations,
            IMarginTradingBlobRepository marginTradingBlobRepository,
            ILog log)
        {
            _migrations = migrations;
            _marginTradingBlobRepository = marginTradingBlobRepository;
            _log = log;
        }
        
        public async Task InvokeAll()
        {
            var migrationVersions = await _marginTradingBlobRepository.ReadAsync<Dictionary<string, int>>(
                                     LykkeConstants.MigrationsBlobContainer, "versions") ?? new Dictionary<string, int>();
            
            foreach (var migration in _migrations)
            {
                var migrationName = migration.GetType().Name;

                try
                {
                    if (!migrationVersions.TryGetValue(migrationName, out var version)
                        || version < migration.Version)
                    {
                        await migration.Invoke();

                        migrationVersions.Remove(migrationName);
                        migrationVersions.Add(migrationName, migration.Version);
                    }
                }
                catch (Exception ex)
                {
                    await _log.WriteFatalErrorAsync(nameof(MigrationService), migrationName, ex, DateTime.UtcNow);
                }
            }

            await _marginTradingBlobRepository.Write(LykkeConstants.MigrationsBlobContainer, "versions", migrationVersions);
        }
    }
}