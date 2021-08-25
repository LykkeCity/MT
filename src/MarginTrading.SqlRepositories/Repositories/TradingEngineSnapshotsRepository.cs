// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Common.Log;
using Dapper;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;

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

        public async Task Add(DateTime tradingDay, string correlationId, DateTime timestamp, string orders,
            string positions, string accounts, string bestFxPrices, string bestTradingPrices)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await _log.WriteInfoAsync(nameof(TradingEngineSnapshotsRepository), nameof(Add),
                    $"Writing {tradingDay:yyyy-MM-dd} snapshot to repository with {correlationId} correlationId.");

                await conn.ExecuteAsync(
                    $@"INSERT INTO {TableName} 
(TradingDay,CorrelationId,Timestamp,Orders,Positions,AccountStats,BestFxPrices,BestPrices) 
VALUES (@TradingDay,@CorrelationId,@Timestamp,@Orders,@Positions,@AccountStats,@BestFxPrices,@BestPrices)",
                    new
                    {
                        TradingDay = tradingDay,
                        CorrelationId = correlationId,
                        Timestamp = timestamp,
                        Orders = orders,
                        Positions = positions,
                        AccountStats = accounts,
                        BestFxPrices = bestFxPrices,
                        BestPrices = bestTradingPrices,
                    }, commandTimeout: _settings.SnapshotInsertTimeoutSec);
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
    }
}