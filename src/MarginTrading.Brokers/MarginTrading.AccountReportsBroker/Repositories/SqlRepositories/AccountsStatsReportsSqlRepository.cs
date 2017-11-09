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
            "[Balance] [numeric](32, 10) NOT NULL, " +
            "[WithdrawTransferLimit] [numeric](32, 10) NOT NULL, " +
            "[MarginCall] [numeric](32, 10) NOT NULL, " +
            "[StopOut] [numeric](32, 10) NOT NULL, " +
            "[TotalCapital] [numeric](32, 10) NOT NULL, " +
            "[FreeMargin] [numeric](32, 10) NOT NULL, " +
            "[MarginAvailable] [numeric](32, 10) NOT NULL, " +
            "[UsedMargin] [numeric](32, 10) NOT NULL, " +
            "[MarginInit] [numeric](32, 10) NOT NULL, " +
            "[PnL] [numeric](32, 10) NOT NULL, " +
            "[OpenPositionsCount] [numeric](32, 10) NOT NULL, " +
            "[MarginUsageLevel] [numeric](32, 10) NOT NULL, " +
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
                    _log?.WriteErrorAsync("AccountsStatsReportsSqlRepository", "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }

        public async Task InsertOrReplaceBatchAsync(IEnumerable<IAccountsStatReport> stats)
        {
            
            using (var conn = new SqlConnection(_settings.Db.ReportsSqlConnString))
            {
                foreach (var stat in stats)
                {
                    var res = conn.ExecuteScalar($"select Id from {TableName} where Id = '{stat.Id}'");
                    string query;
                    if (res == null)
                    {
                        query = $"insert into {TableName} " +
                            "(Id, Date, BaseAssetId, AccountId, ClientId, TradingConditionId, Balance, WithdrawTransferLimit, MarginCall, StopOut, "+
                            "TotalCapital, FreeMargin, MarginAvailable, UsedMargin, MarginInit, PnL, OpenPositionsCount, MarginUsageLevel, IsLive) " +
                            " values " +
                            "(@Id, @Date, @BaseAssetId, @AccountId, @ClientId, @TradingConditionId, @Balance, @WithdrawTransferLimit, @MarginCall, "+
                            "@StopOut, @TotalCapital, @FreeMargin, @MarginAvailable, @UsedMargin, @MarginInit, @PnL, @OpenPositionsCount, @MarginUsageLevel, @IsLive)";
                    }
                    else
                    {
                        query = $"update {TableName} set " +
                            "Date=@Date, BaseAssetId=@BaseAssetId, AccountId=@AccountId, ClientId=@ClientId, TradingConditionId=@TradingConditionId, " +
                            "Balance=@Balance, WithdrawTransferLimit=@WithdrawTransferLimit, MarginCall=@MarginCall, StopOut=@StopOut, TotalCapital=@TotalCapital, " +
                            "FreeMargin=@FreeMargin, MarginAvailable=@MarginAvailable, UsedMargin=@UsedMargin, MarginInit=@MarginInit, PnL=@PnL, " +
                            "OpenPositionsCount=@OpenPositionsCount, MarginUsageLevel=@MarginUsageLevel, IsLive=@IsLive " +
                            " where Id=@Id";
                    }

                    try { await conn.ExecuteAsync(query, stat); }
                    catch (Exception ex)
                    {
                        string msg = $"Error {ex.Message} \n" +
                            "Entity <IAccountsStatReport>: \n" +
                            $" Id:{stat.Id}\n" +
                            $" Date:{stat.Date}\n" +
                            $" AccountId:{stat.AccountId}\n" +
                            $" ClientId:{stat.ClientId}\n" +
                            $" TradingConditionId:{stat.TradingConditionId}\n" +
                            $" BaseAssetId:{stat.BaseAssetId}\n" +
                            $" Balance:{stat.Balance}\n" +
                            $" FreeMargin:{stat.FreeMargin}\n" +
                            $" MarginAvailable:{stat.MarginAvailable}\n" +
                            $" MarginCall:{stat.MarginCall}\n" +
                            $" MarginInit:{stat.MarginInit}\n" +
                            $" MarginUsageLevel:{stat.MarginUsageLevel}\n" +
                            $" PnL:{stat.PnL}\n" +
                            $" StopOut:{stat.StopOut}\n" +
                            $" TotalCapital:{stat.TotalCapital}\n" +
                            $" UsedMargin:{stat.UsedMargin}\n" +
                            $" OpenPositionsCount:{stat.OpenPositionsCount}\n" +
                            $" IsLive:{stat.IsLive}\n" +                            
                            $" WithdrawTransferLimit:{stat.WithdrawTransferLimit}";
                        Exception newException = new Exception(msg);
                        await _log?.WriteErrorAsync("AccountsStatsReportsSqlRepository", "InsertOrReplaceBatchAsync", null, newException);
                        throw newException;
                    }
                }
                
            }
        }
    }
}
