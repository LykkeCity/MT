using Common;
using Common.Log;
using Dapper;
using MarginTrading.AccountHistoryBroker.Repositories.Models;
using MarginTrading.BrokerBase;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.AccountHistoryBroker.Repositories.SqlRepositories
{
    public class AccountTransactionsReportsSqlRepository : IAccountTransactionsReportsRepository
    {
        private const string TableName = "MarginTradingAccountTransactionsReports";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
            "[Id] [nvarchar](64) NOT NULL, " +
            "[Date] [datetime] NOT NULL," +
            "[ClientId] [nvarchar] (64) NOT NULL, " +
            "[AccountId] [nvarchar] (64) NOT NULL, " +
            "[PositionId] [text] NULL, " +
            "[Amount] float NOT NULL, " +
            "[Balance] float NOT NULL, " +
            "[Type] [nvarchar] (50) NOT NULL, " +
            "[Comment] [text] NOT NULL, " +
            "[WithdrawTransferLimit] float NOT NULL, " +
            "[AuditLog] [text] NULL, " +
            "[LegalEntity] [nvarchar] (64) NULL, " +
            "[AmountInUsd] float NOT NULL " +
            "CONSTRAINT[PK_{0}] PRIMARY KEY CLUSTERED ([Id] ASC)" +
            ");";

        private readonly Settings _settings;
        private readonly ILog _log;

        private static readonly string GetColumns =
            string.Join(",", typeof(IAccountTransactionsReport).GetProperties().Select(x => x.Name));

        private static readonly string GetFields =
            string.Join(",", typeof(IAccountTransactionsReport).GetProperties().Select(x => "@" + x.Name));

        private static readonly string GetUpdateClause = string.Join(",",
            typeof(IAccountTransactionsReport).GetProperties().Select(x => "[" + x.Name + "]=@" + x.Name));

        public AccountTransactionsReportsSqlRepository(Settings settings, ILog log)
        {
            _log = log;
            _settings = settings;
            using (var conn = new SqlConnection(_settings.Db.ReportsSqlConnString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync("AccountTransactionsReportsSqlRepository", "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }

        public async Task InsertOrReplaceAsync(IAccountTransactionsReport entity)
        {   
            using (var conn = new SqlConnection(_settings.Db.ReportsSqlConnString))
            {
                try
                {
                    try
                    {
                        await conn.ExecuteAsync(
                            $"insert into {TableName} ({GetColumns}) values ({GetFields})", entity);
                    }
                    catch (SqlException)
                    {
                        await conn.ExecuteAsync(
                            $"update {TableName} set {GetUpdateClause} where Id=@Id", entity); 
                    }
                }
                catch (Exception ex)
                {
                    var msg = $"Error {ex.Message} \n" +
                              "Entity <IAccountTransactionsReport>: \n" +
                              entity.ToJson();
                    await _log?.WriteWarningAsync("AccountTransactionsReportsSqlRepository", "InsertOrReplaceAsync", null, msg);
                    throw new Exception(msg);
                }
            }
        }
    }
}
