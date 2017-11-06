using Common.Log;
using Dapper;
using MarginTrading.AccountReportsBroker.Repositories.Models;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace MarginTrading.AccountReportsBroker.Repositories.SqlRepositories
{
    public class AccountsReportsSqlRepository : IAccountsReportsRepository
    {
        private const string TableName = "ClientAccountsReports";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
            "[Id] [nvarchar](64) NOT NULL, " +
            "[Date] [datetime] NOT NULL, " +
            "[TakerCounterpartyId] [nvarchar] (64) NOT NULL, " +
            "[TakerAccountId] [nvarchar] (64) NOT NULL, " +
            "[BaseAssetId] [nvarchar] (64) NOT NULL, " +
            "[IsLive] [bit] NOT NULL, " +
            "CONSTRAINT[PK_{0}] PRIMARY KEY CLUSTERED ([Id] ASC)" +
            ");";

        private readonly IDbConnection _connection;
        private readonly ILog _log;

        public AccountsReportsSqlRepository(Settings settings, ILog log)
        {
            _log = log;
            _connection = new SqlConnection(settings.Db.ReportsSqlConnString);
            CreateTableIfDoesntExists();
        }

        public async Task InsertOrReplaceAsync(IAccountsReport report)
        {
            string query = $"insert into {TableName} " +
               "(Id, Date, TakerCounterpartyId, TakerAccountId, BaseAssetId, IsLive) " +
               " values " +
               "(@Id, @Date, @TakerCounterpartyId, @TakerAccountId, @BaseAssetId, @IsLive)";

            await _connection.ExecuteAsync(query, report);
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
                _log.WriteErrorAsync("AccountsReportsSqlRepository", "CreateTableIfDoesntExists", null, ex);
                throw;
            }
        }
    }
}
