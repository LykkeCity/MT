using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Dapper;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Common.Services;
using MarginTrading.SqlRepositories.Entities;

namespace MarginTrading.SqlRepositories.Repositories
{
    public class AccountStatRepository : IAccountStatRepository
    {
        private const string TableName = "AccountStatDump";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 @"[Id] [nvarchar](64) NOT NULL PRIMARY KEY,
[PnL] [float] NULL,
[UnrealizedDailyPnl] [float] NULL,
[UsedMargin] [float] NULL,
[MarginInit] [float] NULL,
[OpenPositionsCount] [int] NULL,
[MarginCall1Level] [float] NULL,
[MarginCall2Level] [float] NULL,
[StopoutLevel] [float] NULL,
[WithdrawalFrozenMargin] [float] NULL,
[UnconfirmedMargin] [float] NULL,
[HistoryTimestamp] [datetime] NOT NULL
);";
        
        private static Type DataType => typeof(AccountStatEntity);
        private static readonly string GetColumns = string.Join(",", DataType.GetProperties().Select(x => x.Name));
        private static readonly string GetFields = string.Join(",", DataType.GetProperties().Select(x => "@" + x.Name));

        private readonly string _connectionString;
        private readonly ILog _log;
        private readonly IDateService _dateService;

        /// <summary>
        /// For testing purposes
        /// </summary>
        public AccountStatRepository(){}
        
        public AccountStatRepository(IDateService dateService, string connectionString, ILog log)
        {
            _dateService = dateService;
            _log = log;
            _connectionString = connectionString;
            
            using (var conn = new SqlConnection(_connectionString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(AccountStatRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }
        
        public async Task Dump(IEnumerable<MarginTradingAccount> accounts)
        {
            var reportTime = _dateService.Now();
            var entities = accounts.Select(x => AccountStatEntity.Create(x, reportTime));
            
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
                        $"Failed to dump account stat data at {_dateService.Now():s}", ex);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
}