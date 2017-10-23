using Common.Log;
using Dapper;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
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
            "[Amount] [numeric] (18, 6) NOT NULL, " +
            "[Balance] [numeric] (18, 6) NOT NULL, " +
            "[Type] [nvarchar] (50) NOT NULL, " +
            "[Comment] [text] NOT NULL, " +
            "[WithdrawTransferLimit] [numeric] (18, 6) NOT NULL, " +
            "CONSTRAINT[PK_{0}] PRIMARY KEY CLUSTERED ([Id] ASC)" +
            ");";

        private readonly IDbConnection _connection;
        private readonly ILog _log;

        public AccountTransactionsReportsSqlRepository(MarginSettings settings, ILog log)
        {
            _log = log;
#if DEBUG
            _connection = new SqlConnection(@"Server=.\SQLEXPRESS1;Database=WampTlsLogs;User Id=sa;Password = na123456;");
#else
            _connection = new SqlConnection(settings.Db.ReportsConnString);
#endif
            CreateTableIfDoesntExists();
        }

        public async Task InsertOrReplaceAsync(IAccountTransactionsReport entity)
        {
            string query = $"insert into {TableName} " +
                "(Id, Date, AccountId, ClientId, Amount, Balance, WithdrawTransferLimit, Comment, Type) " +
                " values " +
                "(@Id ,@Date, @AccountId, @ClientId, @Amount, @Balance, @WithdrawTransferLimit, @Comment, @Type)";

            await _connection.ExecuteAsync(query, entity);
        }

        private void CreateTableIfDoesntExists()
        {
            try
            {
                _connection.Open();
                try
                {
                    // Check if table exists
                    var res = _connection.ExecuteScalar($"select top 1 Id from {TableName}");
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
                _log.WriteErrorAsync("AccountTransactionsReportsSqlRepository", "CreateTableIfDoesntExists", null, ex);
                throw;
            }
        }
    }
}
