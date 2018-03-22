using Common;
using Common.Log;
using Dapper;
using MarginTrading.AccountHistoryBroker.Repositories.Models;
using MarginTrading.BrokerBase;
using System;
using System.Data.SqlClient;
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
            "CONSTRAINT[PK_{0}] PRIMARY KEY CLUSTERED ([Id] ASC)" +
            ");";

        private readonly Settings _settings;
        private readonly ILog _log;

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
                var res = conn.ExecuteScalar($"select Id from {TableName} where Id = '{entity.Id}'");
                string query;
                if (res == null)
                {
                    query = $"insert into {TableName} " +
                     "(Id, Date, AccountId, ClientId, Amount, Balance, WithdrawTransferLimit, Comment, Type, PositionId, LegalEntity, AuditLog) " +
                     " values " +
                     "(@Id ,@Date, @AccountId, @ClientId, @Amount, @Balance, @WithdrawTransferLimit, @Comment, @Type, @PositionId, @LegalEntity, @AuditLog)";
                }
                else
                {
                    query = $"update {TableName} set " +
                      "Date=@Date, AccountId=@AccountId, ClientId=@ClientId, Amount=@Amount, Balance=@Balance, " +
                      "WithdrawTransferLimit=@WithdrawTransferLimit, Comment=@Comment, Type=@Type, " +
                      "PositionId = @PositionId, LegalEntity = @LegalEntity, AuditLog = @AuditLog" +
                      " where Id=@Id";
                }
                try { await conn.ExecuteAsync(query, entity); }
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
