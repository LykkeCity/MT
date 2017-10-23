using System.Threading.Tasks;
using MarginTrading.Core;
using Common.Log;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using System;
using MarginTrading.Core.Settings;

namespace MarginTrading.AccountMarginEventsBroker.Repositories.SqlRepositories
{
    internal class AccountMarginEventsReportsSqlRepository : IAccountMarginEventsReportsRepository
    {
        private const string TableName = "AccountMarginEventsReports";
        private const string CreateTableScript = "CREATE TABLE [{0}] (" +
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
            "CONSTRAINT[PK_{0}] PRIMARY KEY CLUSTERED ([EventId] ASC)" +
            ");";

        private readonly IDbConnection _connection;
        private readonly ILog _log;

        public AccountMarginEventsReportsSqlRepository(MarginSettings settings, ILog log)
        {
            _log = log;
#if DEBUG
            _connection = new SqlConnection(@"Server=.\SQLEXPRESS1;Database=WampTlsLogs;User Id=sa;Password = na123456;");

#else
            _connection = new SqlConnection(settings.Db.ReportsConnString);
#endif
            CreateTableIfDoesntExists();
        }

        public async Task InsertOrReplaceAsync(IAccountMarginEventReport report)
        {
            string query = $"insert into {TableName} " +
               "(EventId, ClientId, AccountId, TradingConditionId, Balance, BaseAssetId, EventTime, FreeMargin, IsEventStopout, MarginAvailable, " +
               "MarginCall, MarginInit, MarginUsageLevel, OpenPositionsCount, PnL, StopOut, TotalCapital, UsedMargin, WithdrawTransferLimit)" +
               " values " +
               "(@EventId, @ClientId, @AccountId, @TradingConditionId, @Balance, @BaseAssetId, @EventTime, @FreeMargin, @IsEventStopout, @MarginAvailable, " +
               "@MarginCall, @MarginInit, @MarginUsageLevel, @OpenPositionsCount, @PnL, @StopOut, @TotalCapital, @UsedMargin, @WithdrawTransferLimit)";

            await _connection.ExecuteAsync(query, report);
        }

        private void CreateTableIfDoesntExists()
        {
            try
            {
                // Open connection
                _connection.Open();
                try
                {
                    // Check if table exists
                    var res = _connection.ExecuteScalar($"select top 1 EventId from {TableName}");
                }
                catch (SqlException)
                {                    
                    try
                    {
                        // Create table
                        string query = string.Format(CreateTableScript, TableName);
                        _connection.QueryAsync(query);
                    }
                    catch { throw; }
                }
                finally { _connection.Close(); }
            }
            catch (Exception ex)
            {
                _log.WriteErrorAsync("AccountMarginEventsReportsSqlRepository", "CreateTableIfDoesntExists", null, ex);
                throw;
            }



        }
    }
}
