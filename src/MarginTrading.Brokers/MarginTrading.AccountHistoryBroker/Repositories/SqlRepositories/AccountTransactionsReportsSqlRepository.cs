using Common.Log;
using Dapper;
using MarginTrading.AccountHistoryBroker.Repositories.Models;
using MarginTrading.BrokerBase;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MarginTrading.AccountHistoryBroker.Repositories.SqlRepositories
{
    internal class AccountTransactionsReportsSqlRepository : IAccountTransactionsReportsRepository
    {
        private const string TableName = "MarginTradingAccountTransactionsReports";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
            "[Id] [nvarchar](64) NOT NULL, " +
            "[Date] [datetime] NOT NULL," +
            "[ClientId] [nvarchar] (64) NOT NULL, " +
            "[AccountId] [nvarchar] (64) NOT NULL, " +
            "[PositionId] [text] NULL, " +
            "[Amount] [numeric] (20, 10) NOT NULL, " +
            "[Balance] [numeric] (20, 10) NOT NULL, " +
            "[Type] [nvarchar] (50) NOT NULL, " +
            "[Comment] [text] NOT NULL, " +
            "[WithdrawTransferLimit] [numeric] (20, 10) NOT NULL, " +
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
                    _log.WriteErrorAsync("AccountTransactionsReportsSqlRepository", "CreateTableIfDoesntExists", null, ex);
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
                     "(Id, Date, AccountId, ClientId, Amount, Balance, WithdrawTransferLimit, Comment, Type, PositionId) " +
                     " values " +
                     "(@Id ,@Date, @AccountId, @ClientId, @Amount, @Balance, @WithdrawTransferLimit, @Comment, @Type, @PositionId)";
                }
                else
                {
                    query = $"update {TableName} set " +
                      "Date=@Date, AccountId=@AccountId, ClientId=@ClientId, Amount=@Amount, Balance=@Balance, " +
                      "WithdrawTransferLimit=@WithdrawTransferLimit, Comment=@Comment, Type=@Type, PositionId = @PositionId " +
                      " where Id=@Id";
                }
                try { await conn.ExecuteAsync(query, entity); }
                catch (Exception ex)
                {
                    await _log.WriteErrorAsync("AccountTransactionsReportsSqlRepository", "InsertOrReplaceAsync", null, ex);
                    throw;
                }
            }
        }
    }
}
