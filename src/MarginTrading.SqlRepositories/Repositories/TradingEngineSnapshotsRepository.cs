// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Dapper;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.SqlRepositories.Entities;

namespace MarginTrading.SqlRepositories.Repositories
{
    public class TradingEngineSnapshotsRepository : ITradingEngineSnapshotsRepository
    {
        private const string TableName = "TradingEngineSnapshots";

        private const string CreateTableScript = @"CREATE TABLE [{0}](
[OID] [bigint] NOT NULL IDENTITY (1,1),
[TradingDay] [datetime] NOT NULL,
[CorrelationId] [nvarchar](64) NOT NULL,
[Timestamp] [datetime] NOT NULL,
[Orders] [nvarchar] (MAX) NOT NULL,
[Positions] [nvarchar](MAX) NOT NULL,
[AccountStats] [nvarchar](MAX) NOT NULL,
[BestFxPrices] [nvarchar](MAX) NOT NULL,
[BestPrices] [nvarchar](MAX) NOT NULL,
INDEX IX_{0}_Base (TradingDay, CorrelationId, Timestamp)
);";
        
        private readonly string _connectionString;
        private readonly MarginTradingSettings _settings;
        private readonly ILog _log;
        
        public TradingEngineSnapshotsRepository(MarginTradingSettings settings, ILog log)
        {
            _connectionString = settings.Db.SqlConnectionString;
            _settings = settings;
            _log = log;
            
            using (var conn = new SqlConnection(_connectionString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(TradingEngineSnapshotsRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }

        public async Task<TradingEngineSnapshot> GetLastAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var entities = await connection.QueryAsync<TradingEngineSnapshotEntity>(
                    $"SELECT TOP(1) * FROM {TableName} ORDER BY Timestamp DESC");

                return entities.FirstOrDefault()?.ToDomain();
            }
        }

        public async Task AddAsync(TradingEngineSnapshot tradingEngineSnapshot)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await _log.WriteInfoAsync(nameof(TradingEngineSnapshotsRepository), nameof(AddAsync),
                    $"Writing {tradingEngineSnapshot.TradingDay:yyyy-MM-dd} snapshot to repository with {tradingEngineSnapshot.CorrelationId} correlationId.");

                var entity = new TradingEngineSnapshotEntity(tradingEngineSnapshot);

                await conn.ExecuteAsync(
                    $@"INSERT INTO {TableName} 
(TradingDay,CorrelationId,Timestamp,Orders,Positions,AccountStats,BestFxPrices,BestPrices) 
VALUES (@TradingDay,@CorrelationId,@Timestamp,@Orders,@Positions,@AccountStats,@BestFxPrices,@BestPrices)",
                    entity, commandTimeout: _settings.SnapshotInsertTimeoutSec);
            }
        }
    }
}