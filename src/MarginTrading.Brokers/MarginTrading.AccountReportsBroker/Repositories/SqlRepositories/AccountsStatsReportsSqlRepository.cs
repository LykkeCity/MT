using Common.Log;
using Dapper;
using MarginTrading.AccountReportsBroker.Repositories.Models;
using MarginTrading.BrokerBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MarginTrading.AccountReportsBroker.Repositories.SqlRepositories
{
    public class AccountsStatsReportsSqlRepository : IAccountsStatsReportsRepository
    {
        private const string TableName = "ClientAccountsStatusReports";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
            "[Id] [nvarchar](64) NOT NULL, " +
            "[Date] [datetime] NOT NULL, " +
            "[BaseAssetId] [nvarchar](64) NOT NULL, " +
            "[AccountId] [nvarchar] (64) NOT NULL, " +
            "[ClientId] [nvarchar] (64) NOT NULL, " +
            "[TradingConditionId] [nvarchar] (64) NOT NULL, " +
            "[Balance] [numeric](20, 10) NOT NULL, " +
            "[WithdrawTransferLimit] [numeric](20, 10) NOT NULL, " +
            "[MarginCall] [numeric](20, 10) NOT NULL, " +
            "[StopOut] [numeric](20, 10) NOT NULL, " +
            "[TotalCapital] [numeric](20, 10) NOT NULL, " +
            "[FreeMargin] [numeric](20, 10) NOT NULL, " +
            "[MarginAvailable] [numeric](20, 10) NOT NULL, " +
            "[UsedMargin] [numeric](20, 10) NOT NULL, " +
            "[MarginInit] [numeric](20, 10) NOT NULL, " +
            "[PnL] [numeric](20, 10) NOT NULL, " +
            "[OpenPositionsCount] [numeric](20, 10) NOT NULL, " +
            "[MarginUsageLevel] [numeric](20, 10) NOT NULL, " +
            "[IsLive] [bit] NOT NULL, " +
            "CONSTRAINT[PK_{0}] PRIMARY KEY CLUSTERED ([Id] ASC)" +
            ");";

        private readonly Settings _settings;
        private readonly ILog _log;

        public AccountsStatsReportsSqlRepository(Settings settings, ILog log)
        {
            _log = log;
            _settings = settings;
            using (var conn = new SqlConnection(_settings.Db.ReportsSqlConnString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log.WriteErrorAsync("AccountsStatsReportsSqlRepository", "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }

        public async Task InsertOrReplaceBatchAsync(IEnumerable<IAccountsStatReport> stats)
        {
            string query = $"insert into {TableName} " +
           "(Id, Date, BaseAssetId, AccountId, ClientId, TradingConditionId, Balance, WithdrawTransferLimit, MarginCall, StopOut, TotalCapital, FreeMargin, MarginAvailable, UsedMargin, MarginInit, PnL, OpenPositionsCount, MarginUsageLevel, IsLive) " +
           " values " +
           "(@Id, @Date, @BaseAssetId, @AccountId, @ClientId, @TradingConditionId, @Balance, @WithdrawTransferLimit, @MarginCall, @StopOut, @TotalCapital, @FreeMargin, @MarginAvailable, @UsedMargin, @MarginInit, @PnL, @OpenPositionsCount, @MarginUsageLevel, @IsLive)";
            using (var conn = new SqlConnection(_settings.Db.ReportsSqlConnString))
            {
                try { await conn.ExecuteAsync(query, stats); }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync("AccountsStatsReportsSqlRepository", "InsertOrReplaceBatchAsync", null, ex);
                    throw;
                }
            }
        }
    }
}
