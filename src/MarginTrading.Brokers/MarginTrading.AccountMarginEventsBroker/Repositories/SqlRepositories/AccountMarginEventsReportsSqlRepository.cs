using Common;
using Common.Log;
using Dapper;
using MarginTrading.AccountMarginEventsBroker.Repositories.Models;
using MarginTrading.BrokerBase;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MarginTrading.AccountMarginEventsBroker.Repositories.SqlRepositories
{
    internal class AccountMarginEventsReportsSqlRepository : IAccountMarginEventsReportsRepository
    {
        private const string TableName = "AccountMarginEventsReports";
        private const string CreateTableScript = "CREATE TABLE [{0}] (" +
            "[Id] [nvarchar](64) NOT NULL, " +
            "[EventId] [nvarchar](64) NOT NULL, " +
            "[ClientId] [nvarchar] (64) NOT NULL, " +
            "[AccountId] [nvarchar] (64) NOT NULL, " +
            "[TradingConditionId] [nvarchar] (64) NOT NULL, " +
            "[Balance] float NOT NULL, " +
            "[BaseAssetId] [nvarchar] (64) NOT NULL, " +
            "[EventTime] [datetime] NOT NULL, " +
            "[FreeMargin] float NOT NULL, " +
            "[IsEventStopout] [bit] NOT NULL, " +
            "[MarginAvailable] float NOT NULL, " +
            "[MarginCall] float NOT NULL, " +
            "[MarginInit] float NOT NULL, " +
            "[MarginUsageLevel] float NOT NULL, " +
            "[OpenPositionsCount] float NOT NULL, " +
            "[PnL] float NOT NULL, " +
            "[StopOut] float NOT NULL, " +
            "[TotalCapital] float NOT NULL, " +
            "[UsedMargin] float NOT NULL, " +
            "[WithdrawTransferLimit] float NOT NULL, " +
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
                    _log?.WriteErrorAsync("AccountMarginEventsReportsSqlRepository", "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
         }

        public async Task InsertOrReplaceAsync(IAccountMarginEventReport report)
        {   

            using (var conn = new SqlConnection(_settings.Db.ReportsSqlConnString))
            {
                var res = conn.ExecuteScalar($"select Id from {TableName} where Id = '{report.Id}'");
                string query;
                if (res == null)
                {
                    query = $"insert into {TableName} " +
                        "(Id, EventId, ClientId, AccountId, TradingConditionId, Balance, BaseAssetId, EventTime, FreeMargin, IsEventStopout, MarginAvailable, " +
                        "MarginCall, MarginInit, MarginUsageLevel, OpenPositionsCount, PnL, StopOut, TotalCapital, UsedMargin, WithdrawTransferLimit)" +
                        " values " +
                        "(@Id, @EventId, @ClientId, @AccountId, @TradingConditionId, @Balance, @BaseAssetId, @EventTime, @FreeMargin, @IsEventStopout, @MarginAvailable, " +
                        "@MarginCall, @MarginInit, @MarginUsageLevel, @OpenPositionsCount, @PnL, @StopOut, @TotalCapital, @UsedMargin, @WithdrawTransferLimit)";
                }
                else
                {
                    query = $"update {TableName} set " +
                        "EventId=@EventId, ClientId=@ClientId, AccountId=@AccountId, TradingConditionId=@TradingConditionId, Balance=@Balance, " +
                        "BaseAssetId=@BaseAssetId, EventTime=@EventTime, FreeMargin=@FreeMargin, IsEventStopout=@IsEventStopout, MarginAvailable=@MarginAvailable, " +
                        "MarginCall=@MarginCall, MarginInit=@MarginInit, MarginUsageLevel=@MarginUsageLevel, OpenPositionsCount=@OpenPositionsCount, " +
                        "PnL=@PnL, StopOut=@StopOut, TotalCapital=@TotalCapital, UsedMargin=@UsedMargin, WithdrawTransferLimit=@WithdrawTransferLimit " +
                        " where Id=@Id";
                }

                try { await conn.ExecuteAsync(query, report);  }
                catch (Exception ex)
                {
                    var msg = $"Error {ex.Message} \n" +
                           "Entity <IAccountMarginEventReport>: \n" +
                           report.ToJson();
                    await _log?.WriteWarningAsync("AccountMarginEventsReportsSqlRepository", "InsertOrReplaceAsync",
                        null, msg);
                    throw new Exception(msg);
                }
            }
        }
    }
}
