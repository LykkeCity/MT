using Common.Log;
using Dapper;
using MarginTrading.AccountReportsBroker.Repositories.Models;
using MarginTrading.BrokerBase;
using System;
using System.Collections.Generic;
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
            "[Balance] float NOT NULL, " +
            "[WithdrawTransferLimit] float NOT NULL, " +
            "[MarginCall] float NOT NULL, " +
            "[StopOut] float NOT NULL, " +
            "[TotalCapital] float NOT NULL, " + 
            "[FreeMargin] float NOT NULL, " +
            "[MarginAvailable] float NOT NULL, " +
            "[UsedMargin] float NOT NULL, " +
            "[MarginInit] float NOT NULL, " +
            "[PnL] float NOT NULL, " +
            "[OpenPositionsCount] float NOT NULL, " +
            "[MarginUsageLevel] float NOT NULL, " +
            "[IsLive] [bit] NOT NULL, " +
            "[LegalEntity] [nvarchar] (64) NULL); ";

        private const string CreatePkScript = "ALTER TABLE {0} ADD CONSTRAINT[PK_{0}] PRIMARY KEY CLUSTERED ([Id] ASC);";

        private readonly Settings _settings;
        private readonly ILog _log;

        public AccountsStatsReportsSqlRepository(Settings settings, ILog log)
        {
            _log = log;
            _settings = settings;
            using (var conn = new SqlConnection(_settings.Db.ReportsSqlConnString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript + CreatePkScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync("AccountsStatsReportsSqlRepository", "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }

        public async Task InsertOrReplaceBatchAsync(IEnumerable<IAccountsStatReport> stats)
        {
            const string tempValuesTable = "#tmpValues";
            
            using (var conn = new SqlConnection(_settings.Db.ReportsSqlConnString))
            {
                try
                {
                    await conn.OpenAsync();
                    
                    var tran = conn.BeginTransaction();

                    var createTempTableQuery = string.Format(CreateTableScript, tempValuesTable);
                    await conn.ExecuteAsync(createTempTableQuery, transaction: tran);
                    
                    var tempInsertQuery = GetInsertQuery(tempValuesTable);

                    //insert values into temp table
                    await conn.ExecuteAsync(tempInsertQuery, stats, transaction: tran);

                    var mergeQuery = GetMergeQuery(TableName, tempValuesTable);

                    //merge values from temp to report table
                    await conn.ExecuteAsync(mergeQuery, transaction: tran);

                    tran.Commit();
                }
                catch (SqlException ex)
                {
                    await _log.WriteWarningAsync("AccountsStatsReportsSqlRepository", "InsertOrReplaceBatchAsync", null,
                        ex.ToString());
                }
                catch (Exception ex)
                {
                    await _log.WriteWarningAsync("AccountsStatsReportsSqlRepository", "InsertOrReplaceBatchAsync", null,
                        ex.ToString());
                    throw;
                }
            }
        }

        private string GetInsertQuery(string tableName)
        {
            return $"INSERT INTO {tableName} " +
                   @"(Id, 
                  Date, 
                  BaseAssetId, 
                  AccountId, 
                  ClientId, 
                  TradingConditionId, 
                  Balance, 
                  WithdrawTransferLimit, 
                  MarginCall, 
                  StopOut,  
                  TotalCapital, 
                  FreeMargin, 
                  MarginAvailable, 
                  UsedMargin, 
                  MarginInit, 
                  PnL, 
                  OpenPositionsCount, 
                  MarginUsageLevel, 
                  IsLive,
                  LegalEntity) 
               VALUES
                  (@Id, 
                   @Date, 
                   @BaseAssetId, 
                   @AccountId, 
                   @ClientId, 
                   @TradingConditionId, 
                   @Balance, 
                   @WithdrawTransferLimit, 
                   @MarginCall,
                   @StopOut, 
                   @TotalCapital, 
                   @FreeMargin, 
                   @MarginAvailable, 
                   @UsedMargin,
                   @MarginInit, 
                   @PnL, 
                   @OpenPositionsCount, 
                   @MarginUsageLevel, 
                   @IsLive,
                   @LegalEntity)";
        }

        private string GetMergeQuery(string targetTableName, string sourceTableName)
        {
            return
                "SET NOCOUNT ON; " +
                $"MERGE {targetTableName} AS target " +
                $"USING (SELECT * from {sourceTableName}) AS source " +
                @"(Id, Date, BaseAssetId, AccountId, ClientId, TradingConditionId, Balance, WithdrawTransferLimit, MarginCall, StopOut, TotalCapital, FreeMargin, MarginAvailable, UsedMargin, MarginInit, PnL, OpenPositionsCount, MarginUsageLevel, IsLive, LegalEntity)
            ON (target.Id = source.Id)  
            WHEN MATCHED THEN   
                UPDATE SET Date=source.Date, BaseAssetId=source.BaseAssetId, AccountId=source.AccountId, ClientId=source.ClientId, TradingConditionId=source.TradingConditionId, Balance=source.Balance, WithdrawTransferLimit=source.WithdrawTransferLimit, MarginCall=source.MarginCall, StopOut=source.StopOut, TotalCapital=source.TotalCapital, FreeMargin=source.FreeMargin, MarginAvailable=source.MarginAvailable, UsedMargin=source.UsedMargin, MarginInit=source.MarginInit, PnL=source.PnL, OpenPositionsCount=source.OpenPositionsCount, MarginUsageLevel=source.MarginUsageLevel, IsLive=source.IsLive, LegalEntity=source.LegalEntity   
            WHEN NOT MATCHED THEN  
                INSERT (Id, Date, BaseAssetId, AccountId, ClientId, TradingConditionId, Balance, WithdrawTransferLimit, MarginCall, StopOut, TotalCapital, FreeMargin, MarginAvailable, UsedMargin, MarginInit, PnL, OpenPositionsCount, MarginUsageLevel, IsLive, LegalEntity)  
                VALUES (source.Id, source.Date, source.BaseAssetId, source.AccountId, source.ClientId, source.TradingConditionId, source.Balance, source.WithdrawTransferLimit, source.MarginCall, source.StopOut, source.TotalCapital, source.FreeMargin, source.MarginAvailable, source.UsedMargin, source.MarginInit, source.PnL, source.OpenPositionsCount, source.MarginUsageLevel, source.IsLive, source.LegalEntity);";

        }
    }
}
