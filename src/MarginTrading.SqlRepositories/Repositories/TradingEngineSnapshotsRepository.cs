// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Common.Log;
using Dapper;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.SqlRepositories.Entities;
using SnapshotStatus = MarginTrading.Backend.Core.Snapshots.SnapshotStatus;

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
[Status] [nvarchar](32) constraint TradingEngineSnapshots_Status_Default_Value default 'Final' not null,
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
                try
                {
                    conn.CreateTableIfDoesntExists(CreateTableScript, TableName);
                }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(TradingEngineSnapshotsRepository), "CreateTableIfDoesntExists", null,
                        ex);
                    throw;
                }
            }
        }

        public Task<TradingEngineSnapshot> GetLastAsync() => DoGetLastAsync(null, SnapshotStatus.Final);

        public Task<TradingEngineSnapshot> GetLastDraftAsync(DateTime? tradingDay) => DoGetLastAsync(tradingDay, SnapshotStatus.Draft);

        public async Task AddAsync(TradingEngineSnapshot tradingEngineSnapshot)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await _log.WriteInfoAsync(nameof(TradingEngineSnapshotsRepository), nameof(AddAsync),
                    $"Writing {tradingEngineSnapshot.TradingDay:yyyy-MM-dd} snapshot to repository with {tradingEngineSnapshot.CorrelationId} correlationId.");

                var entity = new TradingEngineSnapshotEntity(tradingEngineSnapshot);

                await conn.ExecuteAsync(
                    $@"INSERT INTO {TableName} 
(TradingDay,CorrelationId,Timestamp,Orders,Positions,AccountStats,BestFxPrices,BestPrices,Status) 
VALUES (@TradingDay,@CorrelationId,@Timestamp,@Orders,@Positions,@AccountStats,@BestFxPrices,@BestPrices,@Status)",
                    entity, commandTimeout: _settings.SnapshotInsertTimeoutSec);
            }
        }

        public async Task<TradingEngineSnapshot> Get(string correlationId)
        {
            var sql = @$"select top(1) TradingDay,
                            CorrelationId,                            
                            Orders as OrdersJson,
                            Positions as PositionsJson,
                            AccountStats as AccountsJson,
                            BestFxPrices as BestFxPricesJson,
                            BestPrices as BestTradingPricesJson,
                            Timestamp
                        from {TableName} where correlationId = @id
                        order by Timestamp desc";

            await using var conn = new SqlConnection(_connectionString);

            var result = await conn.QueryFirstOrDefaultAsync<TradingEngineSnapshot>(sql, new {id = correlationId});
            return result;
        }

        public async Task Delete(string correlationId)
        {
            var sql = @$"delete from {TableName} where correlationId = @id";

            await using var conn = new SqlConnection(_connectionString);

            await conn.ExecuteAsync(sql, new {id = correlationId});
        }

        public async Task<bool> DraftExistsAsync(DateTime tradingDay)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var o = await connection.QuerySingleOrDefaultAsync(
                    $"SELECT TOP 1 OID FROM {TableName} WHERE [TradingDay] = {tradingDay} AND [Status] = '{nameof(SnapshotStatus.Draft)}' ORDER BY [Timestamp] DESC");

                return o != null;
            }
        }

        private async Task<TradingEngineSnapshot> DoGetLastAsync(DateTime? tradingDay, SnapshotStatus status)

        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string ss = new SnapshotStatusString(status);

                var entities = await connection.QueryAsync<TradingEngineSnapshotEntity>(
                    $"SELECT TOP(1) * FROM {TableName} WHERE [Status] = '{ss}' "
                    + (tradingDay.HasValue
                        ? $"AND TradingDay = {tradingDay} "
                        : string.Empty)
                    + "ORDER BY [Timestamp] DESC");

                return entities.FirstOrDefault()?.ToDomain();
            }
        }
    }
}