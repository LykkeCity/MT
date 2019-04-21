using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Dapper;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.SqlRepositories.Repositories
{
    public class TradingEngineSnapshotsRepository : ITradingEngineSnapshotsRepository
    {
        private const string TableName = "TradingEngineSnapshots";

        private const string CreateTableScript = @"CREATE TABLE [{0}](
[OID] [bigint] NOT NULL IDENTITY (1,1),
[CorrelationId] [nvarchar](64) NOT NULL,
[Timestamp] [datetime] NOT NULL,
[Orders] [nvarchar] (MAX) NOT NULL,
[Positions] [nvarchar](MAX) NOT NULL,
[AccountStats] [nvarchar](MAX) NOT NULL,
[BestFxPrices] [nvarchar](MAX) NOT NULL,
[BestPrices] [nvarchar](MAX) NOT NULL,
INDEX IX_{0}_Base (CorrelationId, Timestamp)
);";
        
        private readonly string _connectionString;
        private readonly ILog _log;
        
        public TradingEngineSnapshotsRepository(MarginTradingSettings settings, ILog log)
        {
            _connectionString = settings.Db.SqlConnectionString;
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

        public async Task Add(string correlationId, DateTime timestamp, string orders, string positions, string accounts,
            string bestFxPrices, string bestTradingPrices)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.ExecuteAsync(
                    $@"INSERT INTO {TableName} 
(CorrelationId,Timestamp,Orders,Positions,AccountStats,BestFxPrices,BestPrices) 
VALUES (@CorrelationId,@Timestamp,@Orders,@Positions,@AccountStats,@BestFxPrices,@BestPrices)",
                    new
                    {
                        CorrelationId = correlationId,
                        Timestamp = timestamp,
                        Orders = orders,
                        Positions = positions,
                        AccountStats = accounts,
                        BestFxPrices = bestFxPrices,
                        BestPrices = bestTradingPrices,
                    });
            }
        }
    }
}