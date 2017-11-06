using Common.Log;
using Dapper;
using MarginTrading.AccountMarginEventsBroker.Repositories.Models;
using MarginTrading.BrokerBase;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MarginTrading.AccountMarginEventsBroker.Repositories.SqlRepositories
{
    internal class AccountMarginEventsReportsSqlRepository : IAccountMarginEventsReportsRepository
    {
        private const string TableName = "AccountMarginEventsReports";
        private const string CreateTableScript = "CREATE TABLE [{0}] (" +
            "[Id][nvarchar](64) NOT NULL, " +
            "[EventId][nvarchar](64) NOT NULL, " +
            "[ClientId] [nvarchar] (64) NOT NULL, " +
            "[AccountId] [nvarchar] (64) NOT NULL, " +
            "[TradingConditionId] [nvarchar] (64) NOT NULL, " +
            "[Balance] [numeric] (20, 10) NOT NULL, " +
            "[BaseAssetId] [nvarchar] (64) NOT NULL, " +
            "[EventTime] [datetime] NOT NULL, " +
            "[FreeMargin] [numeric] (20, 10) NOT NULL, " +
            "[IsEventStopout] [bit] NOT NULL, " +
            "[MarginAvailable] [numeric] (20, 10) NOT NULL, " +
            "[MarginCall] [numeric] (20, 10) NOT NULL, " +
            "[MarginInit] [numeric] (20, 10) NOT NULL, " +
            "[MarginUsageLevel] [numeric] (20, 10) NOT NULL, " +
            "[OpenPositionsCount] [numeric] (20, 10) NOT NULL, " +
            "[PnL] [numeric] (20, 10) NOT NULL, " +
            "[StopOut] [numeric] (20, 10) NOT NULL, " +
            "[TotalCapital] [numeric] (20, 10) NOT NULL, " +
            "[UsedMargin] [numeric] (20, 10) NOT NULL, " +
            "[WithdrawTransferLimit] [numeric] (20, 10) NOT NULL, " +
            "CONSTRAINT[PK_{0}] PRIMARY KEY CLUSTERED ([Id] ASC)" +
            ");";

        private readonly Settings _settings;
        private readonly ILog _log;

        public AccountMarginEventsReportsSqlRepository(Settings settings, ILog log)
        {
            _log = log;
            _settings = settings;
            using (var conn = new SqlConnection(_settings.Db.ReportsSqlConnString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log.WriteErrorAsync("AccountMarginEventsReportsSqlRepository", "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
         }

        public async Task InsertOrReplaceAsync(IAccountMarginEventReport report)
        {
            string query = $"insert into {TableName} " +
               "(Id, EventId, ClientId, AccountId, TradingConditionId, Balance, BaseAssetId, EventTime, FreeMargin, IsEventStopout, MarginAvailable, " +
               "MarginCall, MarginInit, MarginUsageLevel, OpenPositionsCount, PnL, StopOut, TotalCapital, UsedMargin, WithdrawTransferLimit)" +
               " values " +
               "(@Id, @EventId, @ClientId, @AccountId, @TradingConditionId, @Balance, @BaseAssetId, @EventTime, @FreeMargin, @IsEventStopout, @MarginAvailable, " +
               "@MarginCall, @MarginInit, @MarginUsageLevel, @OpenPositionsCount, @PnL, @StopOut, @TotalCapital, @UsedMargin, @WithdrawTransferLimit)";

            using (var conn = new SqlConnection(_settings.Db.ReportsSqlConnString))
            {
                try { await conn.ExecuteAsync(query, report);  }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync("AccountMarginEventsReportsSqlRepository", "InsertOrReplaceAsync", null, ex);
                    throw;
                }
            }
        }
    }
}
