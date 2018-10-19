using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Dapper;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.Services;
using MarginTrading.SqlRepositories.Entities;

namespace MarginTrading.SqlRepositories.Repositories
{
    public class OpenPositionsRepository : IOpenPositionsRepository
    {
        private const string TableName = "OpenPositionsDump";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 @"[OID] [bigint] NOT NULL IDENTITY (1,1) PRIMARY KEY,
[Id] [nvarchar](64) NOT NULL,
[Code] [bigint] NULL,
[AssetPairId] [nvarchar] (64) NULL,
[Direction] [nvarchar] (64) NULL,
[Volume] [float] NULL,
[AccountId] [nvarchar] (64) NULL,
[TradingConditionId] [nvarchar] (64) NULL,
[AccountAssetId] [nvarchar] (64) NULL,
[ExpectedOpenPrice] [float] NULL,
[OpenMatchingEngineId] [nvarchar] (64) NULL,
[OpenDate] [datetime] NULL,
[OpenTradeId] [nvarchar] (64) NULL,
[OpenPrice] [float] NULL,
[OpenFxPrice] [float] NULL,
[EquivalentAsset] [nvarchar] (64) NULL,
[OpenPriceEquivalent] [float] NULL,
[RelatedOrders] [nvarchar](1024) NULL,
[LegalEntity] [nvarchar] (64) NULL,
[OpenOriginator] [nvarchar] (64) NULL,
[ExternalProviderId] [nvarchar] (64) NULL,
[SwapCommissionRate] [float] NULL,
[OpenCommissionRate] [float] NULL,
[CloseCommissionRate] [float] NULL,
[CommissionLot] [float] NULL,
[CloseMatchingEngineId] [nvarchar] (64) NULL,
[ClosePrice] [float] NULL,
[CloseFxPrice] [float] NULL,
[ClosePriceEquivalent] [float] NULL,
[StartClosingDate] [datetime] NULL,
[CloseDate] [datetime] NULL,
[CloseOriginator] [nvarchar] (64) NULL,
[CloseReason] [nvarchar] (256) NULL,
[CloseComment] [nvarchar] (256) NULL,
[CloseTrades] [nvarchar] (MAX) NULL,
[LastModified] [datetime] NULL,
[TotalPnL] [float] NULL,
[ChargedPnl] [float] NULL,
[Margin] [float] NULL,
[HistoryTimestamp] [datetime] NOT NULL
);";
        
        private static Type DataType => typeof(OpenPositionEntity);
        private static readonly string GetColumns = string.Join(",", DataType.GetProperties().Select(x => x.Name));
        private static readonly string GetFields = string.Join(",", DataType.GetProperties().Select(x => "@" + x.Name));

        private readonly string _connectionString;
        private readonly ILog _log;
        private readonly IDateService _dateService;

        /// <summary>
        /// For testing purposes
        /// </summary>
        public OpenPositionsRepository(){}
        
        public OpenPositionsRepository(IDateService dateService, string connectionString, ILog log)
        {
            _dateService = dateService;
            _log = log;
            _connectionString = connectionString;
            
            using (var conn = new SqlConnection(_connectionString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(OpenPositionsRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }
        
        public async Task Dump(IEnumerable<Position> openPositions)
        {
            var reportTime = _dateService.Now();
            var entities = openPositions.Select(x => OpenPositionEntity.Create(x, reportTime));
            
            using (var conn = new SqlConnection(_connectionString))
            {
                if (conn.State == ConnectionState.Closed)
                    await conn.OpenAsync();
                
                var transaction = conn.BeginTransaction();
                
                try
                {
                    await conn.ExecuteAsync(
                        $"TRUNCATE TABLE {TableName}",
                        new {},
                        transaction);
                    
                    await conn.ExecuteAsync(
                        $"INSERT INTO {TableName} ({GetColumns}) VALUES ({GetFields})",
                        entities,
                        transaction);
                    
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    await _log.WriteWarningAsync(nameof(AccountMarginFreezingRepository), nameof(Dump),
                        $"Failed to dump open positions data at {_dateService.Now():s}", ex);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
}